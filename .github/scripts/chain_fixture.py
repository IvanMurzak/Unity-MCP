#!/usr/bin/env python3
# CANONICAL COPY - engine repos vendor this file to .github/scripts/chain_fixture.py
# byte-identically (same rule as chain_feed.py, chain-testing 02 section C8). Edit it here,
# then re-vendor; a drifted copy is a defect, and `chain_fixture.py hash --file <path>`
# is what a vendor check compares.
#
# T2 record / replay fixture tool. The format it reads and writes is specified in
# docs/chain-fixtures.md (F1 file, F2 canonicalisation, F3 masking, F4 caps, F5 diff,
# F6 replay); this file and McpPlugin.NullEngine/src/Replay/FixtureLoader.cs are the two
# implementations of F2-F4, kept in step by a differential test
# (McpPlugin.Tests/Chain/CanonicalizeParityTests.cs).
#
# Stdlib only, Python 3.9+: it runs on hosted `python3`, on Unreal Engine's bundled
# Python 3.11 and on the Windows dev box. Nothing here may import a third-party module.
"""Record, canonicalise, diff and hash T2 chain fixtures.

Subcommands
    record        drive an MCP server over Streamable HTTP and write a canonical fixture
    canonicalize  rewrite a fixture (or a raw dump) in canonical form
    diff          compare a committed fixture against a fresh one
    hash          sha256 of a file, or the args_hash of a JSON arguments document

Exit codes
    0  success / fixtures identical
    2  usage error
    3  contract-change (diff found a difference)
    4  schema mismatch, or a fixture over the size cap
    5  record failed to talk to the server
"""

from __future__ import annotations

import argparse
import datetime
import difflib
import hashlib
import json
import os
import re
import sys
import urllib.error
import urllib.request

CHAIN_FIXTURE_VERSION = "1"

#: The only fixture schema this build understands. A fixture carrying anything else is a
#: schema mismatch (exit 4) rather than a contract change: the two files are not comparable.
FIXTURE_SCHEMA = 1

#: F4. A canonical response larger than this is stored as {"$hash": ..., "$bytes": n}.
MAX_RESPONSE_BYTES = 32 * 1024

#: F4. A whole canonical fixture larger than this fails canonicalisation.
MAX_FIXTURE_BYTES = 256 * 1024

EXIT_OK = 0
EXIT_USAGE = 2
EXIT_CONTRACT_CHANGE = 3
EXIT_SCHEMA = 4
EXIT_RECORD_FAILED = 5

RECORDER_MCP_CLIENT = "mcp-client"
RECORDER_IN_PROCESS = "in-process"

#: F3. Meta fields that name WHICH bits produced the fixture rather than what the contract
#: is. Kept in the file for humans, replaced by a placeholder in the comparison form.
PROVENANCE_META = {
    "recorded_at": "<ts>",
    "plugin_version": "<version>",
    "mcp_plugin_version": "<version>",
}

#: F3 (cross-recorder). `meta.recorder` is determined by HOW the fixture was captured, and
#: `response.errorKind` is observable ONLY in-process - the MCP wire drops it
#: (McpPlugin.Server/src/Extension/ExtensionsTool.cs:92-101 maps Status/Content/
#: StructuredContent and nothing else). Masked only under --cross-recorder, so the default
#: diff an engine leg runs stays strict on both.
CROSS_RECORDER_META = {"recorder": "<recorder>"}

#: F1 content-block fields. Anything else an SDK happens to serialise is dropped, so a
#: transport-library upgrade cannot show up as a contract change.
CONTENT_BLOCK_FIELDS = ("type", "text", "data", "mimeType", "resource")

#: F1 tool-line fields, in addition to "kind" and "name".
TOOL_FIELDS = (
    "title",
    "description",
    "inputSchema",
    "outputSchema",
    "readOnlyHint",
    "destructiveHint",
    "idempotentHint",
    "openWorldHint",
)

# ---------------------------------------------------------------------------------------
# F3 masking rules. Applied to STRING VALUES inside a call's `response` subtree, in this
# exact order, plus the key-driven rules below. The C# mirror carries the same table; the
# parity test reddens if either drifts.
# ---------------------------------------------------------------------------------------

RE_TIMESTAMP = re.compile(
    r"\d{4}-\d{2}-\d{2}[Tt ]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:[Zz]|[+-]\d{2}:?\d{2})?"
)
RE_GUID = re.compile(
    r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
)
# A drive letter must not be the tail of a longer word, else "http:" in a URL would match.
RE_WINDOWS_PATH = re.compile(r"(?<![A-Za-z0-9])[A-Za-z]:[\\/][^\s\"'<>|]*")
# The negative lookahead is what keeps "/variable" from being read as "/var" + junk.
RE_POSIX_PATH = re.compile(
    r"(?<![A-Za-z0-9._\-])/(?:Users|home|tmp|var|private|opt|mnt|Volumes)"
    r"(?![A-Za-z0-9])[^\s\"'<>|]*"
)
RE_URL_PORT = re.compile(r"(://[^\s/:\"'<>|]+):\d{1,5}")
RE_DURATION_MS = re.compile(r"\b\d+(?:\.\d+)?\s*ms\b")

#: Key-driven masking: the member's whole value becomes the placeholder, whatever its JSON
#: type, because a pid / port / duration is usually a NUMBER and a regex over strings can
#: never reach it. Deliberately a short, explicit list - a generic name like "ms" would
#: swallow `null-engine/slow`'s `slept_ms`, which echoes an ARGUMENT and is not a
#: measurement, and masking it would silently remove the fixture's ability to discriminate
#: on that field.
MASK_BY_KEY = {
    "pid": "<pid>",
    "process_id": "<pid>",
    "processId": "<pid>",
    "port": "<port>",
    "duration_ms": "<ms>",
    "durationMs": "<ms>",
    "elapsed_ms": "<ms>",
    "elapsedMs": "<ms>",
}


class FixtureError(Exception):
    """A fixture that cannot be read as schema FIXTURE_SCHEMA. Always exit 4."""


# =======================================================================================
# F2 canonical JSON
# =======================================================================================


class RawNumber(str):
    """A JSON number kept as its ORIGINAL token text.

    Re-formatting numbers is the one place where two independent canonicalisers reliably
    disagree (1e3 / 1000 / 1000.0 all round-trip through a float the same way, and the
    shortest-repr rules differ between languages). Preserving the source token removes the
    question: both implementations copy the bytes they were given. Subclasses `str` so the
    stdlib parser hooks can return it directly - which is also why every isinstance check
    below tests RawNumber BEFORE str.
    """

    __slots__ = ()


def _reject_constant(name):
    raise FixtureError("NaN/Infinity is not valid JSON: " + name)


def parse_json(text):
    """Parse JSON preserving number tokens verbatim."""
    return json.loads(
        text,
        parse_int=RawNumber,
        parse_float=RawNumber,
        parse_constant=_reject_constant,
    )


_STRING_ESCAPES = {
    '"': '\\"',
    "\\": "\\\\",
    "\b": "\\b",
    "\f": "\\f",
    "\n": "\\n",
    "\r": "\\r",
    "\t": "\\t",
}


def _write_string(value, out):
    out.append('"')
    for ch in value:
        escape = _STRING_ESCAPES.get(ch)
        if escape is not None:
            out.append(escape)
        elif ch < " ":
            out.append("\\u%04x" % ord(ch))
        else:
            # Non-ASCII is emitted RAW; the file is UTF-8 and both implementations write
            # the same bytes for it. Escaping it instead would mean agreeing on \uXXXX
            # casing and on surrogate-pair splitting, which is more to get wrong.
            out.append(ch)
    out.append('"')


def _write(node, out):
    if isinstance(node, RawNumber):
        out.append(str(node))
    elif node is None:
        out.append("null")
    elif isinstance(node, bool):
        out.append("true" if node else "false")
    elif isinstance(node, str):
        _write_string(node, out)
    elif isinstance(node, int):
        out.append(str(node))
    elif isinstance(node, float):
        raise FixtureError("float values must be constructed as RawNumber")
    elif isinstance(node, list):
        out.append("[")
        for index, item in enumerate(node):
            if index:
                out.append(",")
            _write(item, out)
        out.append("]")
    elif isinstance(node, dict):
        out.append("{")
        # Sorted by the key's UTF-8 BYTES, not by code unit: for the ASCII keys this format
        # uses the two agree, and byte order is the one ordering C# and Python can both
        # express without argument.
        for index, key in enumerate(sorted(node, key=lambda k: k.encode("utf-8"))):
            if index:
                out.append(",")
            _write_string(key, out)
            out.append(":")
            _write(node[key], out)
        out.append("}")
    else:
        raise FixtureError("unsupported JSON value: " + type(node).__name__)


def canonical_dumps(node):
    """Serialise `node` in the F2 canonical form: sorted keys, no insignificant space."""
    out = []
    _write(node, out)
    return "".join(out)


def sha256_hex(data):
    return hashlib.sha256(data).hexdigest()


def numbers_as_text(node):
    """Replace every JSON number by its raw token text, as a string, recursively.

    MEASURED, not defensive. The SignalR payload serializer this plugin family uses converts
    numbers to strings on the way to the plugin: a client that sends ``{"kb": 1}`` reaches the
    tool as ``{"kb": "1"}``, and it applies inside nested objects too
    (``{"payload":{"number":7,"flag":true}}`` arrives as ``{"payload":{"number":"7","flag":true}}``
    - booleans and strings keep their kind, numbers do not). A hash taken over the CLIENT's view
    of the arguments therefore could never match one taken inside the plugin, and replay would
    miss every call that carries a number. Hashing this form makes the two views agree.

    It is a widening: ``1`` and ``"1"`` hash the same. That is precisely the identification the
    transport already makes, so nothing that could be told apart at replay time is lost.

    Only the HASH uses this form. The fixture keeps the arguments as recorded, so a diff still
    shows the battery's real values.
    """
    if isinstance(node, RawNumber):
        return str(node)
    if isinstance(node, bool) or node is None or isinstance(node, str):
        return node
    if isinstance(node, int) or isinstance(node, float):
        return str(node)
    if isinstance(node, list):
        return [numbers_as_text(item) for item in node]
    if isinstance(node, dict):
        return dict((key, numbers_as_text(value)) for key, value in node.items())
    return node


def args_hash(args):
    """F1. sha256 of the canonical args JSON, prefixed with its algorithm."""
    return "sha256:" + sha256_hex(canonical_dumps(numbers_as_text(args or {})).encode("utf-8"))


# =======================================================================================
# F3 masking
# =======================================================================================


def mask_string(value):
    value = RE_TIMESTAMP.sub("<ts>", value)
    value = RE_GUID.sub("<guid>", value)
    value = RE_WINDOWS_PATH.sub("<path>", value)
    value = RE_POSIX_PATH.sub("<path>", value)
    value = RE_URL_PORT.sub(r"\1:<port>", value)
    value = RE_DURATION_MS.sub("<ms>", value)
    return value


def mask_node(node):
    """Apply F3 to a response subtree. Keys are never rewritten, only values."""
    if isinstance(node, RawNumber) or isinstance(node, bool) or node is None:
        return node
    if isinstance(node, str):
        return mask_string(node)
    if isinstance(node, list):
        return [mask_node(item) for item in node]
    if isinstance(node, dict):
        masked = {}
        for key, value in node.items():
            placeholder = MASK_BY_KEY.get(key)
            masked[key] = placeholder if placeholder is not None else mask_node(value)
        return masked
    return node


# =======================================================================================
# F1 fixture model
# =======================================================================================


def read_lines(path):
    """Read a fixture (or a raw dump) as a list of JSON objects.

    Trailing CR is tolerated on read - a checkout with the wrong line-ending setting must
    surface as the diff it is, not as a parse error - but every write is LF.
    """
    try:
        with open(path, "rb") as handle:
            raw = handle.read()
    except OSError as error:
        raise FixtureError("cannot read %s: %s" % (path, error))

    # A leading BOM is stripped, NOT tolerated by accident: the C# side reads fixtures with
    # File.ReadAllText(path, Encoding.UTF8), whose StreamReader removes one on its own. Without
    # this the two implementations would DISAGREE about which files they accept - replay would
    # serve a BOM'd fixture happily while the diff that gates CI refused it - and "the two agree"
    # is the whole promise of this format.
    text = raw.decode("utf-8-sig")
    objects = []
    for number, line in enumerate(text.split("\n"), start=1):
        line = line.rstrip("\r")
        if not line.strip():
            continue
        try:
            parsed = parse_json(line)
        except ValueError as error:
            raise FixtureError("%s:%d is not valid JSON: %s" % (path, number, error))
        if not isinstance(parsed, dict):
            raise FixtureError("%s:%d is not a JSON object" % (path, number))
        objects.append(parsed)

    if not objects:
        raise FixtureError("%s is empty" % path)
    return objects


def split_fixture(objects, path="<fixture>"):
    """Validate the schema and split the lines into (meta, tools, calls)."""
    meta = objects[0]
    if meta.get("kind") != "meta":
        raise FixtureError("%s: the first line must be {\"kind\":\"meta\"}" % path)

    schema = meta.get("schema")
    if str(schema) != str(FIXTURE_SCHEMA):
        raise FixtureError(
            "%s: schema mismatch - this build understands schema %d, the fixture declares %r"
            % (path, FIXTURE_SCHEMA, schema)
        )

    tools = []
    calls = []
    for index, entry in enumerate(objects[1:], start=2):
        kind = entry.get("kind")
        if kind == "tool":
            if not entry.get("name"):
                raise FixtureError("%s:%d tool line has no name" % (path, index))
            tools.append(entry)
        elif kind == "call":
            if not entry.get("name"):
                raise FixtureError("%s:%d call line has no name" % (path, index))
            calls.append(entry)
        else:
            raise FixtureError("%s:%d unknown kind %r" % (path, index, kind))

    return meta, tools, calls


def _ordered_meta(meta):
    out = {"schema": FIXTURE_SCHEMA, "kind": "meta"}
    for key, value in meta.items():
        if key in ("schema", "kind"):
            continue
        out[key] = value
    return out


def _canonical_tool(entry):
    out = {"kind": "tool", "name": entry["name"]}
    for field in TOOL_FIELDS:
        value = entry.get(field)
        if value is not None:
            out[field] = value
    return out


def _canonical_response(response, drop_error_kind=False):
    """Normalise a response envelope to the F1 fields, then mask and cap it.

    `drop_error_kind` is applied BEFORE the cap, not after: a hashed response's digest
    covers whatever the envelope held, so removing a field afterwards would leave two
    cross-recorder fixtures with different digests for the same oversize payload.
    """
    if not isinstance(response, dict):
        raise FixtureError("a call's response must be a JSON object")

    if "$hash" in response:
        # An already-capped response is passed through untouched: canonicalising a
        # canonical fixture must be a no-op (the idempotence test asserts exactly that),
        # and re-hashing a hash would change the value on every pass.
        if "$bytes" not in response:
            raise FixtureError("a capped response must carry both $hash and $bytes")
        return {"$hash": response["$hash"], "$bytes": response["$bytes"]}

    status = response.get("status")
    if status not in ("success", "error"):
        raise FixtureError("a call's response.status must be 'success' or 'error', got %r" % status)

    out = {"status": status}

    blocks = response.get("content")
    if blocks is not None:
        if not isinstance(blocks, list):
            raise FixtureError("a call's response.content must be an array")
        normalised = []
        for block in blocks:
            if not isinstance(block, dict):
                raise FixtureError("a content block must be a JSON object")
            kept = {}
            for field in CONTENT_BLOCK_FIELDS:
                value = block.get(field)
                if value is not None:
                    kept[field] = value
            normalised.append(kept)
        out["content"] = normalised

    if response.get("structuredContent") is not None:
        out["structuredContent"] = response["structuredContent"]

    # Recorder-scoped: present for an in-process capture, absent for an MCP-client one.
    if not drop_error_kind and response.get("errorKind") is not None:
        out["errorKind"] = response["errorKind"]

    masked = mask_node(out)
    encoded = canonical_dumps(masked).encode("utf-8")
    if len(encoded) > MAX_RESPONSE_BYTES:
        return {"$hash": "sha256:" + sha256_hex(encoded), "$bytes": len(encoded)}
    return masked


def _canonical_call(entry, drop_error_kind=False):
    args = entry.get("args")
    if args is None:
        args = {}
    if not isinstance(args, dict):
        raise FixtureError("a call's args must be a JSON object")

    return {
        "kind": "call",
        "name": entry["name"],
        "args": args,
        # Always RECOMPUTED, never trusted: it is a derived field, and a recorder that
        # emitted a stale one would otherwise silently break replay lookup.
        "args_hash": args_hash(args),
        "response": _canonical_response(entry.get("response"), drop_error_kind),
    }


def _sort_key(text):
    return text.encode("utf-8")


def canonicalize(objects, path="<fixture>", for_diff=False, cross_recorder=False):
    """F2 + F3 + F4. Returns the canonical fixture TEXT (LF separated, trailing LF)."""
    meta, tools, calls = split_fixture(objects, path)

    meta = _ordered_meta(meta)
    if for_diff:
        for key, placeholder in PROVENANCE_META.items():
            if key in meta:
                meta[key] = placeholder
        if cross_recorder:
            for key, placeholder in CROSS_RECORDER_META.items():
                if key in meta:
                    meta[key] = placeholder

    lines = [canonical_dumps(meta)]

    for tool in sorted((_canonical_tool(t) for t in tools), key=lambda t: _sort_key(t["name"])):
        lines.append(canonical_dumps(tool))

    drop_error_kind = for_diff and cross_recorder
    canonical_calls = [_canonical_call(c, drop_error_kind) for c in calls]

    for call in sorted(canonical_calls, key=lambda c: (_sort_key(c["name"]), _sort_key(c["args_hash"]))):
        lines.append(canonical_dumps(call))

    text = "\n".join(lines) + "\n"
    encoded = text.encode("utf-8")
    if len(encoded) > MAX_FIXTURE_BYTES:
        raise FixtureError(
            "fixture too large: trim the battery (%d bytes, cap %d)"
            % (len(encoded), MAX_FIXTURE_BYTES)
        )
    return text


def write_text(path, text):
    directory = os.path.dirname(os.path.abspath(path))
    if directory and not os.path.isdir(directory):
        os.makedirs(directory, exist_ok=True)
    # newline="" so this writes LF on every platform; the format fixes the line ending.
    with open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)


# =======================================================================================
# record - drive a live MCP server over Streamable HTTP
# =======================================================================================


class McpTransportError(Exception):
    """The server could not be driven. Always exit 5 - never a contract verdict."""


class McpClient:
    """The smallest Streamable-HTTP JSON-RPC client that can drive tools/list + tools/call.

    Deliberately not an MCP SDK: this file is vendored into repositories that must run it
    with nothing installed, so stdlib urllib is the whole transport.
    """

    def __init__(self, base_url, timeout=60.0):
        self.url = base_url.rstrip("/") + "/mcp"
        self.timeout = timeout
        self.session_id = None
        self._next_id = 0

    def _post(self, payload):
        body = json.dumps(payload).encode("utf-8")
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
        }
        if self.session_id:
            headers["Mcp-Session-Id"] = self.session_id
        request = urllib.request.Request(self.url, data=body, headers=headers, method="POST")
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as response:
                text = response.read().decode("utf-8")
                issued = response.headers.get("Mcp-Session-Id")
        except urllib.error.HTTPError as error:
            detail = error.read().decode("utf-8", "replace")
            raise McpTransportError(
                "POST %s -> HTTP %s: %s" % (self.url, error.code, detail[:2000])
            )
        except (urllib.error.URLError, OSError) as error:
            raise McpTransportError("POST %s failed: %s" % (self.url, error))

        if issued and not self.session_id:
            self.session_id = issued
        return text

    @staticmethod
    def unwrap(body):
        """Pull the JSON out of an SSE frame's data: lines (one document per reply)."""
        if "data:" not in body:
            return body
        chunks = []
        for line in body.split("\n"):
            line = line.rstrip("\r")
            if line.startswith("data:"):
                chunks.append(line[5:].strip())
        return "".join(chunks) if chunks else body

    def request(self, method, params=None):
        self._next_id += 1
        payload = {"jsonrpc": "2.0", "id": self._next_id, "method": method}
        if params is not None:
            payload["params"] = params
        body = self.unwrap(self._post(payload))
        if not body.strip():
            raise McpTransportError("%s returned an empty body" % method)
        try:
            document = parse_json(body)
        except ValueError as error:
            raise McpTransportError("%s returned unparsable JSON: %s" % (method, error))
        if "error" in document:
            raise McpTransportError("%s returned a JSON-RPC error: %s" % (method, document["error"]))
        if "result" not in document:
            raise McpTransportError("%s returned no result: %s" % (method, body[:500]))
        return document["result"]

    def notify(self, method, params=None):
        payload = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            payload["params"] = params
        self._post(payload)

    def initialize(self):
        result = self.request(
            "initialize",
            {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "chain-fixture-recorder", "version": CHAIN_FIXTURE_VERSION},
            },
        )
        if not self.session_id:
            raise McpTransportError("the server did not mint an Mcp-Session-Id on initialize")
        self.notify("notifications/initialized")
        return result

    def list_tools(self):
        tools = []
        cursor = None
        while True:
            params = {"cursor": cursor} if cursor else {}
            result = self.request("tools/list", params)
            tools.extend(result.get("tools") or [])
            cursor = result.get("nextCursor")
            if not cursor:
                return tools

    def call_tool(self, name, arguments):
        return self.request("tools/call", {"name": name, "arguments": arguments or {}})


def _tool_line_from_mcp(entry):
    annotations = entry.get("annotations") or {}
    line = {"kind": "tool", "name": entry.get("name")}
    title = annotations.get("title")
    if title is None:
        title = entry.get("title")
    values = {
        "title": title,
        "description": entry.get("description"),
        "inputSchema": entry.get("inputSchema"),
        "outputSchema": entry.get("outputSchema"),
        "readOnlyHint": annotations.get("readOnlyHint"),
        "destructiveHint": annotations.get("destructiveHint"),
        "idempotentHint": annotations.get("idempotentHint"),
        "openWorldHint": annotations.get("openWorldHint"),
    }
    for key, value in values.items():
        if value is not None:
            line[key] = value
    return line


def _response_from_mcp(result):
    """Normalise an MCP CallToolResult into the F1 response envelope.

    `errorKind` is NOT set here and cannot be: the server maps a plugin response onto
    CallToolResult{IsError, Content, StructuredContent} and drops the kind
    (McpPlugin.Server/src/Extension/ExtensionsTool.cs:92-101). That asymmetry is exactly
    what --cross-recorder exists for.
    """
    response = {"status": "error" if result.get("isError") else "success"}
    blocks = result.get("content")
    if blocks is not None:
        response["content"] = blocks
    if result.get("structuredContent") is not None:
        response["structuredContent"] = result["structuredContent"]
    return response


def load_battery(path):
    try:
        with open(path, "rb") as handle:
            document = parse_json(handle.read().decode("utf-8"))
    except OSError as error:
        raise FixtureError("cannot read battery %s: %s" % (path, error))
    except ValueError as error:
        raise FixtureError("battery %s is not valid JSON: %s" % (path, error))

    if not isinstance(document, dict):
        raise FixtureError("battery %s must be a JSON object" % path)
    if str(document.get("schema")) != str(FIXTURE_SCHEMA):
        raise FixtureError(
            "battery %s: schema mismatch - expected %d, got %r"
            % (path, FIXTURE_SCHEMA, document.get("schema"))
        )
    calls = document.get("calls")
    if not isinstance(calls, list) or not calls:
        raise FixtureError("battery %s must carry a non-empty 'calls' array" % path)
    for entry in calls:
        if not isinstance(entry, dict) or not entry.get("name"):
            raise FixtureError("battery %s: every call needs a 'name'" % path)
    return calls


def utc_now_iso():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def cmd_record(args):
    battery = load_battery(args.battery)
    client = McpClient(args.server_url, timeout=args.timeout)
    client.initialize()

    objects = [
        {
            "schema": FIXTURE_SCHEMA,
            "kind": "meta",
            "engine": args.engine,
            "engine_version": args.engine_version,
            "surface": args.surface,
            "plugin_version": args.plugin_version,
            "mcp_plugin_version": args.mcp_plugin_version,
            "recorded_at": utc_now_iso(),
            "recorder": RECORDER_MCP_CLIENT,
        }
    ]

    for entry in client.list_tools():
        objects.append(_tool_line_from_mcp(entry))

    for entry in battery:
        arguments = entry.get("args") or {}
        result = client.call_tool(entry["name"], arguments)
        objects.append(
            {
                "kind": "call",
                "name": entry["name"],
                "args": arguments,
                "response": _response_from_mcp(result),
            }
        )

    write_text(args.out, canonicalize(objects, args.out))
    tools = sum(1 for o in objects if o.get("kind") == "tool")
    calls = sum(1 for o in objects if o.get("kind") == "call")
    print("recorded tools=%d calls=%d out=%s" % (tools, calls, args.out))
    return EXIT_OK


def cmd_canonicalize(args):
    objects = read_lines(args.input)
    text = canonicalize(
        objects, args.input, for_diff=args.for_diff, cross_recorder=args.cross_recorder
    )
    if args.out:
        write_text(args.out, text)
        print("canonicalized %s -> %s (%d bytes)" % (args.input, args.out, len(text.encode("utf-8"))))
    else:
        sys.stdout.write(text)
    return EXIT_OK


def cmd_diff(args):
    committed = canonicalize(
        read_lines(args.committed), args.committed, for_diff=True, cross_recorder=args.cross_recorder
    )
    fresh = canonicalize(
        read_lines(args.fresh), args.fresh, for_diff=True, cross_recorder=args.cross_recorder
    )
    if committed == fresh:
        return EXIT_OK

    print("contract-change: %s and %s differ" % (args.committed, args.fresh))
    diff = difflib.unified_diff(
        committed.splitlines(keepends=True),
        fresh.splitlines(keepends=True),
        fromfile=args.committed,
        tofile=args.fresh,
        n=1,
    )
    sys.stdout.writelines(diff)
    return EXIT_CONTRACT_CHANGE


def cmd_hash(args):
    try:
        if args.file:
            with open(args.file, "rb") as handle:
                print("sha256:" + sha256_hex(handle.read()))
            return EXIT_OK

        if args.args_file:
            with open(args.args_file, "rb") as handle:
                document = parse_json(handle.read().decode("utf-8"))
        else:
            document = parse_json(args.args)
    except OSError as error:
        raise FixtureError("cannot read: %s" % error)
    except ValueError as error:
        raise FixtureError("not valid JSON: %s" % error)

    if not isinstance(document, dict):
        raise FixtureError("arguments must be a JSON object")
    print(args_hash(document))
    return EXIT_OK


def build_parser():
    parser = argparse.ArgumentParser(prog="chain_fixture.py", description=__doc__)
    parser.add_argument("--version", action="version", version=CHAIN_FIXTURE_VERSION)
    sub = parser.add_subparsers(dest="command")

    record = sub.add_parser("record", help="record a fixture from a live MCP server")
    record.add_argument("--server-url", required=True, help="MCP server base URL, e.g. http://127.0.0.1:8080")
    record.add_argument("--battery", required=True, help="battery JSON listing the calls to make")
    record.add_argument("--engine", required=True)
    record.add_argument("--engine-version", required=True)
    record.add_argument("--surface", required=True, choices=["editor", "runtime"])
    record.add_argument("--out", required=True)
    record.add_argument("--plugin-version", default="unknown")
    record.add_argument("--mcp-plugin-version", default="unknown")
    record.add_argument("--timeout", type=float, default=60.0)
    record.set_defaults(handler=cmd_record)

    canon = sub.add_parser("canonicalize", help="rewrite a fixture in canonical form")
    canon.add_argument("input")
    canon.add_argument("--out", help="output path; stdout when omitted")
    canon.add_argument(
        "--for-diff",
        action="store_true",
        help="also mask the provenance meta fields - the exact form `diff` compares",
    )
    canon.add_argument(
        "--cross-recorder",
        action="store_true",
        help="additionally neutralise the fields determined by the recorder kind",
    )
    canon.set_defaults(handler=cmd_canonicalize)

    diff = sub.add_parser("diff", help="compare a committed fixture with a fresh one")
    diff.add_argument("committed")
    diff.add_argument("fresh")
    diff.add_argument("--cross-recorder", action="store_true")
    diff.set_defaults(handler=cmd_diff)

    hash_cmd = sub.add_parser("hash", help="sha256 of a file, or the args_hash of arguments")
    group = hash_cmd.add_mutually_exclusive_group(required=True)
    group.add_argument("--file", help="sha256 of the file's raw bytes")
    group.add_argument("--args", help="args_hash of an inline JSON object")
    group.add_argument("--args-file", help="args_hash of a JSON object read from a file")
    hash_cmd.set_defaults(handler=cmd_hash)

    return parser


def _force_utf8_stdio():
    """Make stdout/stderr UTF-8 and newline-transparent.

    Not defensive tidying: on Windows the console encoding is cp1252, so
    `canonicalize` writing a fixture that carries CJK or an emoji to stdout dies with
    UnicodeEncodeError, and any stdout redirection would translate LF to CRLF - which
    would corrupt the very bytes this format fixes.
    """
    for stream in (sys.stdout, sys.stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if reconfigure is None:
            continue
        try:
            reconfigure(encoding="utf-8", newline="")
        except (ValueError, OSError):
            pass


def main(argv=None):
    _force_utf8_stdio()
    parser = build_parser()
    args = parser.parse_args(argv)
    if not getattr(args, "handler", None):
        parser.print_help(sys.stderr)
        return EXIT_USAGE

    try:
        return args.handler(args)
    except FixtureError as error:
        sys.stderr.write("chain_fixture: %s\n" % error)
        return EXIT_SCHEMA
    except McpTransportError as error:
        sys.stderr.write("chain_fixture: record failed: %s\n" % error)
        return EXIT_RECORD_FAILED


if __name__ == "__main__":
    sys.exit(main())
