#!/usr/bin/env python3
# VENDORED from ai-game-dev-software .scripts/chain/leg/chain_feed.py — edit there, re-vendor
"""The one contract every per-repo chain leg implements (`p2-chain-feed-script` §C1…§C9).

ONE stdlib-only file, copied byte-identically into each of the nine node repos as
`.github/scripts/chain_feed.py` (§C8, the `cascade_pr_flow.py` precedent). A leg is dispatched
with the lock as its single input, self-builds its lower layers from the lock's SHAs into a
LOCAL feed, applies the same override the P1 modules apply locally, asserts identity, and
uploads one leg record per job.

It runs on hosted ubuntu `python3`, on the Unreal runners' UE-bundled Python 3.11, and on the
`windows-local` / `nvme-mac-arm64` system Pythons — so: Python 3.9+, no `import yaml`, no
third-party imports, and no import of `.scripts/chain/*` (a foreign checkout has none). The
parity with the P1 override modules is therefore a TEST, never an import
(`.scripts/chain/tests/test_feed_parity.py`).

Exit codes: **0** ok (including the deliberate no-op of an ordinary run) · **2** a refusal
(no node, bad lock, sha mismatch, missing toolchain, missing artifact) · **3** an identity
proof came back `ok: false`, so the job is RED even if its tests passed (§C5).

Two rules the whole file is written to. Every exit status is read from
`CompletedProcess.returncode` — never through a pipe. And every proof `record` emits names the
ARTIFACT it was read from (`project.assets.json`, a DLL sha256, `chain-identity.json`), never
an exit code.
"""

CHAIN_FEED_VERSION = "4"

import argparse
import hashlib
import http.client
import io
import json
import os
import platform
import re
import shlex
import shutil
import struct
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from pathlib import Path

EXIT_OK = 0
EXIT_REFUSED = 2
EXIT_IDENTITY = 3

#: B7: `workflow_dispatch` accepts a 65,535-character payload. The leg refuses well below the
#: hard cap so a lock that grew past the limit is a named refusal here rather than an opaque
#: GitHub-side rejection at dispatch time.
MAX_LOCK_CHARS = 60000
LOCK_HASH8_RE = re.compile(r"^[0-9a-f]{8}\Z")

# ----------------------------------------------------------------------
# lock input validation — every lock-sourced scalar, strictly, at LOAD time
# ----------------------------------------------------------------------
#
# The lock is untrusted input: it arrives as a `workflow_dispatch` string or out of a PR BODY
# (§C7), and its values reach a `shell=True` recipe, a git argv, a feed PATH, an XML attribute
# and `$GITHUB_ENV` (`CHAIN_BUILD_PROPS` / `CHAIN_WS_VERSION`, which consumer workflows
# interpolate into their own `run:` steps). So each value is checked against the one shape it
# may have BEFORE anything reads it, and a mismatch is an exit-2 refusal naming the field —
# never a value quietly carried to a sink. Every pattern is anchored with `\Z` (or used with
# `fullmatch`): Python's `$` also matches before a trailing newline.

LOCK_SHA_RE = re.compile(r"^[0-9a-f]{40}\Z")
#: `lock.ws_version_for`: `<base_version>-ws.g<sha[:8]>`. The character set admits no shell,
#: cmd.exe, path-separator or XML metacharacter, and no leading `-`.
LOCK_WS_VERSION_RE = re.compile(r"^[0-9]+(?:\.[0-9]+){0,3}(?:-[0-9A-Za-z.-]+)?-ws\.g([0-9a-f]{8})\Z")
LOCK_STATES = ("main", "released", "sha", "branch", "pr")
LOCK_HASH_RE = re.compile(r"^sha256:[0-9a-f]{64}\Z")
#: A ref as `lock.py` records it (`refs/heads/<branch>`, `refs/pull/<n>/head`, a sha). This file
#: never reads it, so its shape is git's own ref grammar, the grammar `lock.parse_state` accepts,
#: not a narrower charset. A git-legal branch such as `fix/#12` must not hard-block its lock. No
#: control character, whitespace, `~^:?*[\`, `..` or leading `-`. A future reader that pastes it
#: into a shell must quote it (`shell_value`).
LOCK_REF_RE = re.compile(r"^(?!-)(?!.*\.\.)[^\s~^:?*\[\\\x00-\x1f\x7f]+\Z")
_CONTROL_CHARS_RE = re.compile(r"[\x00-\x1f\x7f]")

#: §C7 — the train path. A `pull_request` carrying a `train/` label takes its lock from the
#: fenced block in the PR BODY, read out of the event payload (never `gh`, so the workflow
#: needs no `pull-requests: read`).
TRAIN_LABEL_PREFIX = "train/"
LOCK_FENCE = "chain-lock"

# ----------------------------------------------------------------------
# generated-file constants — byte-identical to the P1 override modules
# ----------------------------------------------------------------------
#
# These strings are duplicated from `.scripts/chain/overrides/{nuget,pnpm}.py` ON PURPOSE and
# their equality is pinned by `test_feed_parity.py`. A leg that wrote a *slightly different*
# `Directory.Build.targets` would be testing something the dev box never tests, which is the
# whole failure class D1 exists to kill — so the marker text still names the P1 module even
# though this copy wrote the file.

NUGET_CONFIG_NAME = "nuget.config"
TARGETS_NAME = "Directory.Build.targets"
NUGET_PROPERTY = "UseWorkspaceSources"
NUGET_SOURCE_KEY = "workspace-local"
NUGET_ORG_URL = "https://api.nuget.org/v3/index.json"
NUGET_PACKAGES_ENV = "NUGET_PACKAGES"
GENERATED_MARKER = "GENERATED by chain.py (chain/overrides/nuget.py)"

PNPM_BEGIN = "# >>> chain override — generated by .scripts/chain/overrides/pnpm.py; NEVER COMMIT >>>"
PNPM_END = "# <<< chain override <<<"
PNPM_INSTALL_CMD = "pnpm install --no-frozen-lockfile"
PNPM_PINS = ("package.json", "pnpm-workspace.yaml", "pnpm-lock.yaml")

UNITY_PACKAGE_PREFIX = "com.IvanMurzak."
UNITY_LIB_ENTRY = "lib/netstandard2.1/{name}"
UNITY_MANIFEST_NAME = ".nuget-installed.json"

SERVER_MARKER_NAME = "chain-identity.json"
SERVER_MARKER_FIELDS = ("ws_version", "sha", "rid", "publish_cmd", "exe_sha256", "product_version")

#: A single NuGet version. Anything carrying range syntax, a float or whitespace is refused
#: rather than pasted into a `Version=` attribute where it would become a range of its own.
#: Anchored with `\Z`, never `$`: in Python `$` also matches BEFORE a trailing newline, so
#: `"5.4.0\n"` would pass a `$`-anchored `match` and carry the newline into whatever it reaches.
_VERSION_RE = re.compile(r"^[0-9]+(?:\.[0-9]+){0,3}(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?\Z")
_PLACEHOLDER_RE = re.compile(r"\{([A-Za-z_][A-Za-z0-9_]*)\}")
_EXPR_RE = re.compile(r"\$\{\{(.*?)\}\}", re.DOTALL)

#: Git-for-Windows' own runtime treats an `X:` prefix in argv as an MSYS path and can mangle
#: `HEAD:<path>`; every git call this file makes sets it, exactly as `chain.lock.GitGh` does.
_GIT_ENV = {"MSYS_NO_PATHCONV": "1"}


# ----------------------------------------------------------------------
# RECIPES — the manifest's own build/pack lines, embedded (D2)
# ----------------------------------------------------------------------
#
# A foreign checkout has no `.chain/manifest.yml`, so the leg carries the recipes it needs.
# The manifest stays the source of truth: `chain/feed.py recipes-check` compares this table
# against `.chain/manifest.yml` and exits 2 on any drift, so the copy can never rot silently.

RECIPES = {
    "reflectornet": {
        "path": "shared/ReflectorNet",
        "slug": "IvanMurzak/ReflectorNet",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build ReflectorNet.sln -c Release -p:Version={ws_version}",
        "pack": [
            "dotnet pack ReflectorNet/ReflectorNet.csproj --no-build --configuration Release --output {feed} -p:Version={ws_version}",
        ],
        "artifacts": {
            "nupkg": {"kind": "nupkg", "id": "com.IvanMurzak.ReflectorNet"},
        },
        "consumes": [],
    },
    "mcp-plugin-dotnet": {
        "path": "shared/MCP-Plugin-dotnet",
        "slug": "IvanMurzak/MCP-Plugin-dotnet",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build McpPlugin.sln -c Release -p:Version={ws_version}",
        "pack": [
            "dotnet pack McpPlugin.Common/McpPlugin.Common.csproj --no-build --configuration Release --output {feed} -p:Version={ws_version}",
            "dotnet pack McpPlugin/McpPlugin.csproj --no-build --configuration Release --output {feed} -p:Version={ws_version}",
            "dotnet pack McpPlugin.Server/McpPlugin.Server.csproj --no-build --configuration Release --output {feed} -p:Version={ws_version}",
        ],
        "artifacts": {
            "mcpplugin": {"kind": "nupkg", "id": "com.IvanMurzak.McpPlugin"},
            "common": {"kind": "nupkg", "id": "com.IvanMurzak.McpPlugin.Common"},
            "server": {"kind": "nupkg", "id": "com.IvanMurzak.McpPlugin.Server"},
        },
        "consumes": [
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "nuget",
                "pins": [
                    "McpPlugin/McpPlugin.csproj",
                    "McpPlugin.Common/McpPlugin.Common.csproj",
                    "McpPlugin.Server/McpPlugin.Server.csproj",
                ],
            },
        ],
    },
    "gamedev-mcp-server": {
        "path": "shared/GameDev-MCP-Server",
        "slug": "IvanMurzak/GameDev-MCP-Server",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build com.IvanMurzak.GameDev.MCP.Server.csproj -c Release -p:Version={ws_version}",
        "pack": [
            "dotnet pack com.IvanMurzak.GameDev.MCP.Server.csproj --no-build --configuration Release --output {feed} -p:Version={ws_version}",
            "dotnet publish com.IvanMurzak.GameDev.MCP.Server.csproj -c Release -r {rid} --self-contained true -p:PublishSingleFile=true -o {server_out}/{rid} -p:Version={ws_version}",
        ],
        "artifacts": {
            "nupkg": {"kind": "nupkg", "id": "com.IvanMurzak.GameDev.MCP.Server"},
            "binary": {
                "kind": "server-binary", "id": "gamedev-mcp-server",
                "rids": ["win-x64", "linux-x64", "osx-arm64"],
            },
        },
        "consumes": [
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "nuget", "pins": ["com.IvanMurzak.GameDev.MCP.Server.csproj"],
            },
            {
                "ref": "mcp-plugin-dotnet/server", "node": "mcp-plugin-dotnet", "artifact": "server",
                "mode": "nuget", "pins": ["com.IvanMurzak.GameDev.MCP.Server.csproj"],
            },
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "nuget", "pins": ["com.IvanMurzak.GameDev.MCP.Server.csproj"],
            },
        ],
    },
    "cli-core": {
        "path": "shared/AI-Game-Dev-CLI-Core",
        "slug": "IvanMurzak/AI-Game-Dev-CLI-Core",
        "ecosystem": "npm",
        "kind": "repo",
        "build": "npm ci && npm run typecheck && npm run build",
        "pack": [
            "npm pkg set version={ws_version} && npm ci && npm run build && npm pack --pack-destination {npm_feed}",
        ],
        "artifacts": {
            "tgz": {"kind": "tgz", "id": "@baizor/gamedev-cli-core"},
        },
        "consumes": [],
        # A TWIN is a checkout one JOB drives beside its own, not a package it restores: the
        # mixed-language concurrency suite runs MCP-Plugin-dotnet's C# harness from source
        # (`concurrency-suite.yml`, the `MCP_PLUGIN_DOTNET_REF` clone). Under a lock it is cloned
        # at the lock's sha. Mirrors the manifest leg's `twins:` (`recipes-check`).
        "twins": [
            {"node": "mcp-plugin-dotnet", "workflow": "concurrency-suite.yml", "job": "suite"},
        ],
    },
    "unity-mcp": {
        "path": "engines/unity/Unity-MCP",
        "slug": "IvanMurzak/Unity-MCP",
        "ecosystem": "unity",
        "kind": "repo",
        "build": "cd cli && npm ci && npm run build",
        "pack": [
            "cd cli && npm ci && npm pkg set version={ws_version} && npm pack --pack-destination {npm_feed}",
        ],
        "artifacts": {
            "cli": {"kind": "tgz", "id": "unity-mcp-cli"},
        },
        "consumes": [
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "unity-dll",
                "pins": [
                    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/DependencyResolver/NuGetConfig.cs",
                    "Unity-MCP-Plugin/Assets/Plugins/NuGet/.nuget-installed.json",
                ],
                "drops": [
                    "Unity-MCP-Plugin/Assets/Plugins/NuGet",
                    "Unity-Tests/2022.3.62f3/Assets/Plugins/NuGet",
                    "Unity-Tests/2023.2.22f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.3.1f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.5.0b3/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.6.0a2/Assets/Plugins/NuGet",
                ],
            },
            {
                "ref": "mcp-plugin-dotnet/common", "node": "mcp-plugin-dotnet", "artifact": "common",
                "mode": "unity-dll",
                "pins": ["Unity-MCP-Plugin/Assets/Plugins/NuGet/.nuget-installed.json"],
                "drops": [
                    "Unity-MCP-Plugin/Assets/Plugins/NuGet",
                    "Unity-Tests/2022.3.62f3/Assets/Plugins/NuGet",
                    "Unity-Tests/2023.2.22f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.3.1f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.5.0b3/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.6.0a2/Assets/Plugins/NuGet",
                ],
            },
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "unity-dll",
                "pins": [
                    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/DependencyResolver/NuGetConfig.cs",
                    "Unity-MCP-Plugin/Assets/Plugins/NuGet/.nuget-installed.json",
                ],
                "drops": [
                    "Unity-MCP-Plugin/Assets/Plugins/NuGet",
                    "Unity-Tests/2022.3.62f3/Assets/Plugins/NuGet",
                    "Unity-Tests/2023.2.22f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.3.1f1/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.5.0b3/Assets/Plugins/NuGet",
                    "Unity-Tests/6000.6.0a2/Assets/Plugins/NuGet",
                ],
            },
            {
                "ref": "gamedev-mcp-server/binary", "node": "gamedev-mcp-server", "artifact": "binary",
                "mode": "server-binary", "env": "UNITY_MCP_SERVER_PATH",
                "pins": [
                    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs",
                    "cli/src/utils/server-version.ts",
                ],
            },
            {
                "ref": "cli-core/tgz", "node": "cli-core", "artifact": "tgz",
                "mode": "npm", "path": "cli", "pins": ["cli/package.json"],
            },
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "nuget", "pins": ["Unity-MCP-Plugin/Tests~/EngineFree/EngineFree.csproj"],
            },
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "nuget", "pins": ["Unity-MCP-Plugin/Tests~/EngineFree/EngineFree.csproj"],
            },
        ],
    },
    "godot-mcp": {
        "path": "engines/godot/Godot-MCP",
        "slug": "IvanMurzak/Godot-MCP",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build Godot-MCP.sln --configuration Debug",
        "pack": [
            "cd cli && npm ci && npm pkg set version={ws_version} && npm pack --pack-destination {npm_feed}",
        ],
        "artifacts": {
            "cli": {"kind": "tgz", "id": "godot-cli"},
        },
        "consumes": [
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "nuget",
                "pins": [
                    "Godot-MCP.csproj",
                    "Godot-MCP.Tests/Godot-MCP.Tests.csproj",
                    "Godot-Tests/Godot-Tests.csproj",
                ],
            },
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "nuget",
                "pins": [
                    "Godot-MCP.csproj",
                    "Godot-MCP.Tests/Godot-MCP.Tests.csproj",
                    "Godot-Tests/Godot-Tests.csproj",
                ],
            },
            {
                "ref": "gamedev-mcp-server/binary", "node": "gamedev-mcp-server", "artifact": "binary",
                "mode": "server-binary", "env": "GODOT_MCP_SERVER_PATH",
                "pins": [
                    "addons/godot_mcp/Runtime/Connection/GodotMcpServerView.cs",
                    "cli/src/utils/server-source.ts",
                ],
            },
            {
                "ref": "cli-core/tgz", "node": "cli-core", "artifact": "tgz",
                "mode": "npm", "path": "cli", "pins": ["cli/package.json"],
            },
        ],
    },
    "unreal-mcp": {
        "path": "engines/unreal/Unreal-MCP",
        "slug": "IvanMurzak/Unreal-MCP",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build bridge/Unreal-MCP-Bridge.sln --configuration Debug",
        "pack": [
            "cd cli && npm ci && npm pkg set version={ws_version} && npm pack --pack-destination {npm_feed}",
        ],
        "artifacts": {
            "cli": {"kind": "tgz", "id": "unreal-mcp-cli"},
        },
        "consumes": [
            {
                "ref": "reflectornet/nupkg", "node": "reflectornet", "artifact": "nupkg",
                "mode": "nuget", "pins": ["bridge/src/com.IvanMurzak.Unreal.MCP.Bridge.csproj"],
            },
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "nuget", "pins": ["bridge/src/com.IvanMurzak.Unreal.MCP.Bridge.csproj"],
            },
            {
                "ref": "gamedev-mcp-server/binary", "node": "gamedev-mcp-server", "artifact": "binary",
                "mode": "server-binary", "env": "UNREAL_MCP_SERVER_PATH",
                "pins": [
                    "UnrealMCP/Source/UnrealMcpEditor/Private/Server/UnrealMcpServerManager.cpp",
                    "cli/src/lib/server-version.ts",
                ],
            },
            {
                "ref": "cli-core/tgz", "node": "cli-core", "artifact": "tgz",
                "mode": "npm", "path": "cli", "pins": ["cli/package.json"],
            },
        ],
    },
    "app": {
        "path": "app/AI-Game-Dev-App",
        "slug": "IvanMurzak/AI-Game-Dev-App",
        "ecosystem": "pnpm",
        "kind": "repo",
        "build": "pnpm install --frozen-lockfile && pnpm build && pnpm typecheck",
        "pack": [],
        "artifacts": {},
        "consumes": [
            {
                "ref": "null-engine/host", "node": "null-engine", "artifact": "host",
                "mode": "project", "pins": [],
            },
            {
                "ref": "cli-core/tgz", "node": "cli-core", "artifact": "tgz",
                "mode": "pnpm", "pins": ["packages/core/package.json"],
            },
            {
                "ref": "unity-mcp/cli", "node": "unity-mcp", "artifact": "cli",
                "mode": "pnpm",
                "pins": ["packages/core/package.json", "packages/desktop/package.json"],
            },
            {
                "ref": "godot-mcp/cli", "node": "godot-mcp", "artifact": "cli",
                "mode": "pnpm",
                "pins": ["packages/core/package.json", "packages/desktop/package.json"],
            },
            {
                "ref": "unreal-mcp/cli", "node": "unreal-mcp", "artifact": "cli",
                "mode": "pnpm",
                "pins": ["packages/core/package.json", "packages/desktop/package.json"],
            },
        ],
    },
    "cloud-ags": {
        "path": "cloud/AI-Game-Dev-Server",
        "slug": "IvanMurzak/AI-Game-Dev-Server",
        "ecosystem": "dotnet",
        "kind": "repo",
        "build": "dotnet build mcp-server/tests/McpServer.Tests/McpServer.Tests.csproj -c Release",
        "pack": [],
        "artifacts": {},
        "consumes": [
            {
                "ref": "null-engine/host", "node": "null-engine", "artifact": "host",
                "mode": "project", "pins": [],
            },
            {
                "ref": "mcp-plugin-dotnet/server", "node": "mcp-plugin-dotnet", "artifact": "server",
                "mode": "nuget", "pins": ["mcp-server/McpServer.csproj"],
            },
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "nuget", "pins": ["mcp-server/McpServer.csproj"],
            },
        ],
    },
    "null-engine": {
        "path": "shared/MCP-Plugin-dotnet",
        "slug": "IvanMurzak/MCP-Plugin-dotnet",
        "ecosystem": "dotnet",
        "kind": "fixture",
        "build": "dotnet build McpPlugin.NullEngine/McpPlugin.NullEngine.csproj -c Release",
        "pack": [],
        "artifacts": {
            "host": {"kind": "exe", "id": "McpPlugin.NullEngine", "pending": True},
        },
        "consumes": [
            {
                "ref": "mcp-plugin-dotnet/mcpplugin", "node": "mcp-plugin-dotnet", "artifact": "mcpplugin",
                "mode": "project", "pins": [],
            },
            {
                "ref": "gamedev-mcp-server/binary", "node": "gamedev-mcp-server", "artifact": "binary",
                "mode": "server-binary", "env": "CHAIN_SERVER_PATH", "pins": [],
            },
        ],
    },
}

#: Which nodes a `p2-dispatch-*` row vendors this file into (`kind: repo`).
VENDOR_NODES = tuple(sorted(nid for nid, body in RECIPES.items() if body.get("kind") == "repo"))

#: Unity is the one leg whose workflow has no `concurrency:` today and needs a NEW block
#: (`04` §2). Everything else either keeps its existing expression or needs none.
NEEDS_NEW_CONCURRENCY = ("unity-mcp",)

#: The `actions/upload-artifact` major the fragment emits: the newest one a node workflow already
#: runs (MCP-Plugin-dotnet `test-pull-request.yml`). §C5 lets a repo keep its own major (>= v4).
UPLOAD_ARTIFACT_MAJOR = "v7"

#: The interpreter probe both fragment resolvers RUN. It imports C-extension modules (a
#: half-installed interpreter reports a version but cannot import them) and prints its own
#: `sys.executable` with forward slashes, which is what the resolver exports — never the
#: candidate's NAME. It contains no quote character of either kind, so it survives a bash AND a
#: pwsh single-quoted argument unchanged. The module list is the set a leg actually loads: `select`
#: and `socket` (subprocess and `urllib`), `ssl` (every HTTPS fetch, incl. `fetch-fixtures`),
#: `zlib` (every nupkg / tarball read) and `ctypes` — the one macOS framework-Python build that
#: omits `_ctypes` fails here, at the resolver, instead of mid-leg (`p2-replay-leg`).
CHAIN_PY_PROBE = (
    "import sys, select, socket, ssl, zlib, ctypes, hashlib; sys.version_info >= (3, 9) or sys.exit(1); "
    "print(sys.executable.replace(chr(92), chr(47)))"
)

#: `--edges` selectors that name neither a mode, a producer node nor a `<node>/<artifact>` ref.
#: `self` = this node's OWN pack (what a root leg proves); `none` = the §C6 sha assertion and a
#: record, nothing overridden and nothing proven (a job that consumes nothing from the chain).
SCOPE_SELF = "self"
SCOPE_NONE = "none"

#: `--edges twin:<node>` selects a declared twin (RECIPES `twins:`). A twin is OPT-IN: no
#: `--edges` at all does NOT select it, because it belongs to ONE job — cloning it into every
#: other job of the node would demand a twin proof those jobs can never produce.
SCOPE_TWIN_PREFIX = "twin:"
TWIN_ENV_PREFIX = "CHAIN_TWIN_"


class Refusal(Exception):
    """Exit 2 with one line. Never a traceback."""


# ----------------------------------------------------------------------
# small utilities
# ----------------------------------------------------------------------


def as_posix_abs(path):
    """Absolute, forward-slashed. `C:/Projects/...`, never `C:\\Projects` or `/c/...`."""
    return str(Path(path).resolve()).replace("\\", "/")


def sha256_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha256_file(path):
    digest = hashlib.sha256()
    with open(str(path), "rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def log(line):
    print(line, flush=True)


def read_text(path):
    """Read with newline translation OFF so a CRLF file survives a surgical edit."""
    with io.open(str(path), "r", encoding="utf-8", newline="") as handle:
        return handle.read()


def write_text(path, text, newline=""):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    with io.open(str(path), "w", encoding="utf-8", newline=newline) as handle:
        handle.write(text)


def host_rid():
    """`win-x64` / `linux-x64` / `osx-arm64` — the RID this runner publishes for."""
    system = platform.system()
    if system == "Windows":
        return "win-x64"
    if system == "Darwin":
        return "osx-arm64" if platform.machine().lower() in ("arm64", "aarch64") else "osx-x64"
    return "linux-x64"


#: The characters cmd.exe treats as syntax even inside a double-quoted argument (`%`/`!`
#: expansion, `^` escape, `"` quote toggling) or outside one (`&`, `|`, `<`, `>`).
_CMD_METACHARS = '&|<>^%!"'


def shell_value(name, value, windows=None):
    """One placeholder value made safe to paste into a `shell=True` recipe.

    POSIX `/bin/sh`: `shlex.quote` — a value made only of `[A-Za-z0-9@%+=:,./_-]` (every
    validated ws version and every ordinary runner path) comes back unchanged, so the plan text
    is the same as before; anything else is single-quoted. Windows `cmd.exe` has no quoting that
    neutralises `%`/`!`/`^`, so there the value must already be free of cmd metacharacters (the
    lock values are, by `validate_lock`); a runner path with whitespace or parentheses is
    double-quoted. CR, LF and NUL are refused on both — no quoting survives a line break.
    """
    text = str(value)
    if any(ch in text for ch in "\r\n\x00"):
        raise Refusal("placeholder {%s} value %r carries a line break or NUL; refusing to build a shell command" % (name, text))
    if windows is None:
        windows = os.name == "nt"
    if windows:
        bad = sorted(set(text) & set(_CMD_METACHARS))
        if bad:
            raise Refusal(
                "placeholder {%s} value %r carries cmd.exe metacharacter(s) %s; refusing to build a "
                "shell command" % (name, text, " ".join(bad))
            )
        return '"%s"' % text if any(ch.isspace() or ch in "()" for ch in text) else text
    return shlex.quote(text)


def substitute(command, values, quote=None):
    """Replace every `{token}`; refuse on one this file does not define or cannot fill.

    `quote(name, value)` — `shell_value` for a command that reaches a shell — is applied to
    each value as it is pasted in; the recipe text itself is trusted (it is this file's own).
    """
    names = {m.group(1) for m in _PLACEHOLDER_RE.finditer(command)}
    unknown = sorted(names - set(values))
    if unknown:
        raise Refusal(
            "unknown placeholder(s) %s in %r" % (", ".join("{%s}" % u for u in unknown), command)
        )
    empty = sorted(name for name in names if not values[name])
    if empty:
        raise Refusal(
            "placeholder(s) %s in %r have no value in this lock"
            % (", ".join("{%s}" % e for e in empty), command)
        )
    quote = quote or (lambda _name, value: value)
    return _PLACEHOLDER_RE.sub(lambda m: quote(m.group(1), values[m.group(1)]), command)


def topological_order(node_ids):
    """Producers before consumers over RECIPES; ties break on node id (plan reproducibility)."""
    scope = set(node_ids)
    producers = {}
    for nid in scope:
        producers[nid] = {
            edge["node"] for edge in RECIPES[nid]["consumes"] if edge["node"] in scope
        }
    order = []
    remaining = dict(producers)
    while remaining:
        ready = sorted(nid for nid, deps in remaining.items() if not deps)
        if not ready:
            raise Refusal(
                "the consumption graph has a cycle; nodes still blocked: "
                + ", ".join(sorted(remaining))
            )
        for nid in ready:
            order.append(nid)
            del remaining[nid]
        for deps in remaining.values():
            deps.difference_update(ready)
    return order


# ----------------------------------------------------------------------
# §C1 / §C7 — where the lock comes from
# ----------------------------------------------------------------------


def _event_payload():
    path = os.environ.get("GITHUB_EVENT_PATH")
    if not path or not Path(path).is_file():
        return {}
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def train_labels(payload):
    labels = ((payload.get("pull_request") or {}).get("labels")) or []
    out = []
    for label in labels:
        name = label.get("name") if isinstance(label, dict) else str(label)
        if name and str(name).startswith(TRAIN_LABEL_PREFIX):
            out.append(str(name))
    return out


def extract_fenced_lock(body):
    """The ```chain-lock fenced JSON block of a PR body, or `None`.

    Deliberately a scan for the fence rather than a regex over the whole body: a train PR body
    is prose written by a human and may contain other fenced blocks.
    """
    if not body:
        return None
    lines = str(body).replace("\r\n", "\n").replace("\r", "\n").split("\n")
    collecting = False
    buffer = []
    for line in lines:
        stripped = line.strip()
        if not collecting:
            if stripped.startswith("```") and stripped[3:].strip() == LOCK_FENCE:
                collecting = True
            continue
        if stripped.startswith("```"):
            return "\n".join(buffer)
        buffer.append(line)
    return None


def load_lock_text(args):
    """`(lock text, hash8 or None, source)` — or `(None, None, reason)` for an ordinary run."""
    explicit = getattr(args, "lock", None)
    if explicit:
        return explicit, os.environ.get("CHAIN_LOCK_HASH8") or None, "argument"
    lock_file = getattr(args, "lock_file", None)
    if lock_file:
        return Path(lock_file).read_text(encoding="utf-8"), os.environ.get("CHAIN_LOCK_HASH8") or None, "file"
    env_lock = os.environ.get("CHAIN_LOCK")
    if env_lock and env_lock.strip():
        return env_lock, os.environ.get("CHAIN_LOCK_HASH8") or None, "CHAIN_LOCK input"

    payload = _event_payload()
    labels = train_labels(payload)
    if labels:
        body = (payload.get("pull_request") or {}).get("body") or ""
        fenced = extract_fenced_lock(body)
        if fenced and fenced.strip():
            return fenced, os.environ.get("CHAIN_LOCK_HASH8") or None, "PR body"
        raise Refusal(
            "this pull request carries %s but its body has no ```%s fenced lock block (C7)"
            % (", ".join(labels), LOCK_FENCE)
        )
    return None, None, "no lock (ordinary run)"


def _lock_field_refusal(where, value, shape):
    # `%r` escapes CR/LF and quotes the value, so the refusal line itself cannot be split.
    return Refusal("lock %s = %r is not %s; refusing the whole lock before any value is used" % (where, value, shape))


def _lock_string(where, value):
    if not isinstance(value, str):
        raise _lock_field_refusal(where, value, "a string")
    if _CONTROL_CHARS_RE.search(value):
        raise _lock_field_refusal(where, value, "free of control characters (CR, LF, NUL, ...)")
    return value


def validate_lock(lock):
    """Refuse (exit 2) any lock whose scalars are not exactly the shape `lock.py` writes.

    Runs in `build_context` straight after the JSON parse, BEFORE the lock hash, the node's sha,
    the recipes or any writer reads a value — so a value that would change a shell command, a
    git argv, a feed path, `$GITHUB_ENV` or `$GITHUB_OUTPUT` never reaches one:

    * `nodes` keys — the node ids this file knows (`RECIPES`, which `recipes-check` pins to the
      manifest); `follows` names one of them too;
    * `sha` — exactly 40 lowercase hex (never an option-shaped `--upload-pack=...` git argv);
    * `ws_version` — `<version>-ws.g<8 hex>`, and those 8 hex are the node's own `sha[:8]`
      whenever the node carries a sha (`lock.ws_version_for`);
    * `version` / `base_version` — a plain version (`_VERSION_RE`);
    * `state` — one of `LOCK_STATES`; `ref` — a ref-name character set;
    * `lock_hash` — `sha256:<64 hex>` when present.
    """
    nodes = lock.get("nodes")
    lock_hash = lock.get("lock_hash")
    if lock_hash not in (None, ""):
        if not isinstance(lock_hash, str) or not LOCK_HASH_RE.match(lock_hash):
            raise _lock_field_refusal("lock_hash", lock_hash, "sha256:<64 lowercase hex>")
    for node_id, entry in nodes.items():
        where = "nodes.%s" % (node_id,)
        if node_id not in RECIPES:
            raise _lock_field_refusal(
                "nodes key", node_id, "a node id this file knows (%s)" % ", ".join(sorted(RECIPES))
            )
        if not isinstance(entry, dict):
            raise _lock_field_refusal(where, entry, "an object")
        for key, value in entry.items():
            if value is None:
                continue
            if isinstance(value, (dict, list)):
                raise _lock_field_refusal("%s.%s" % (where, key), value, "a scalar")
            if isinstance(value, str):
                _lock_string("%s.%s" % (where, key), value)
        state = entry.get("state")
        if state is not None and state not in LOCK_STATES:
            raise _lock_field_refusal(where + ".state", state, "one of %s" % ", ".join(LOCK_STATES))
        sha = entry.get("sha")
        if sha is not None and not (isinstance(sha, str) and LOCK_SHA_RE.match(sha)):
            raise _lock_field_refusal(where + ".sha", sha, "40 lowercase hex characters")
        for key in ("version", "base_version"):
            value = entry.get(key)
            if value is not None and not (isinstance(value, str) and _VERSION_RE.match(value)):
                raise _lock_field_refusal("%s.%s" % (where, key), value, "a plain version")
        ws_version = entry.get("ws_version")
        if ws_version is not None:
            match = LOCK_WS_VERSION_RE.match(ws_version) if isinstance(ws_version, str) else None
            if match is None:
                raise _lock_field_refusal(where + ".ws_version", ws_version, "<version>-ws.g<8 lowercase hex>")
            if sha and match.group(1) != sha[:8]:
                raise _lock_field_refusal(
                    where + ".ws_version", ws_version, "stamped with this node's own sha (-ws.g%s)" % sha[:8]
                )
        ref = entry.get("ref")
        if ref is not None and not (isinstance(ref, str) and LOCK_REF_RE.match(ref)):
            raise _lock_field_refusal(where + ".ref", ref, "a git ref name (no whitespace, ~^:?*[\\, `..` or leading -)")
        follows = entry.get("follows")
        if follows is not None and follows not in RECIPES:
            raise _lock_field_refusal(where + ".follows", follows, "a node id this file knows")
    return lock


class LegContext(object):
    """Everything a subcommand is allowed to need, resolved once."""

    def __init__(self, node, job, lock, lock_hash8, source, checkout, runner_temp, scope=None):
        self.node = node
        self.job = job
        #: `None` = no `--edges` filter (every edge in scope), else a frozenset of selectors.
        self.scope = scope
        self.lock = lock
        self.lock_hash = str(lock.get("lock_hash") or "")
        self.lock_hash8 = lock_hash8
        self.source = source
        self.checkout = Path(checkout)
        self.runner_temp = Path(runner_temp)

    # -- paths ----------------------------------------------------------
    @property
    def artifacts(self):
        return self.checkout / ".artifacts"

    @property
    def feed_nuget(self):
        return self.artifacts / "nuget"

    @property
    def feed_npm(self):
        return self.artifacts / "npm"

    @property
    def feed_server(self):
        return self.artifacts / "server"

    @property
    def feed_unity_dll(self):
        return self.artifacts / "unity-dll"

    @property
    def nuget_packages_dir(self):
        return as_posix_abs(self.artifacts / "nuget-cache" / self.lock_hash8)

    @property
    def chain_temp(self):
        return self.runner_temp / "chain"

    @property
    def lock_path(self):
        return self.chain_temp / "lock.json"

    @property
    def state_path(self):
        return self.chain_temp / "leg-state.json"

    def clone_dir(self, node_id):
        return self.chain_temp / node_id

    def twin_dir(self, node_id):
        """Apart from `clone_dir`: a lower-layer build of the same node must never rmtree it."""
        return self.chain_temp / "twin" / node_id

    # -- lock -----------------------------------------------------------
    def node_entry(self, node_id):
        return (self.lock.get("nodes") or {}).get(node_id) or {}

    def ws_version(self, node_id):
        return self.node_entry(node_id).get("ws_version")

    def sha(self, node_id):
        return self.node_entry(node_id).get("sha")

    def command_env(self, extra=None):
        env = dict(os.environ)
        env[NUGET_PROPERTY] = "true"
        env[NUGET_PACKAGES_ENV] = self.nuget_packages_dir
        env.update(extra or {})
        return env


def _default_checkout():
    return Path(os.environ.get("GITHUB_WORKSPACE") or os.getcwd())


def _default_runner_temp():
    value = os.environ.get("RUNNER_TEMP")
    if value:
        return Path(value)
    return Path(tempfile.gettempdir())


def parse_lock_payload(lock_text, source, enforce_limits=True):
    """The ONE lock-parse gate every lock-consuming command shares: size cap (B7), JSON, a
    `nodes` object, then `validate_lock`.

    `build_context` (every leg command) and `cmd_fetch_fixtures` both load a lock, and two
    hand-rolled copies of this gate already drifted once — the fetch-fixtures copy had reduced
    the B7 message to a bare marker, so the same oversized lock refused with two different
    sentences. The gate lives here so the message has exactly one home.
    """
    if enforce_limits and len(lock_text) > MAX_LOCK_CHARS:
        raise Refusal(
            "lock input is %d characters, over the %d-character limit (B7: the "
            "workflow_dispatch payload hard cap is 65,535)" % (len(lock_text), MAX_LOCK_CHARS)
        )
    try:
        lock = json.loads(lock_text)
    except ValueError as exc:
        raise Refusal("lock is not JSON (%s): %s" % (source, exc))
    if not isinstance(lock, dict) or not isinstance(lock.get("nodes"), dict):
        raise Refusal("lock (%s) has no `nodes` object" % source)
    validate_lock(lock)
    return lock


def build_context(args, enforce_limits=True):
    """Resolve the lock (§C1/§C7) and assert §C6, or return `None` for an ordinary run."""
    lock_text, hash8, source = load_lock_text(args)
    if lock_text is None:
        log("chain: no lock (ordinary run)")
        return None
    lock = parse_lock_payload(lock_text, source, enforce_limits=enforce_limits)

    if not hash8:
        hash8 = str(lock.get("lock_hash") or "").split(":")[-1][:8]
    if not LOCK_HASH8_RE.match(hash8 or ""):
        raise Refusal(
            "lock_hash8 %r is not exactly 8 lowercase hex characters; refusing rather than guessing the "
            "run-name and cache key" % (hash8,)
        )

    node = getattr(args, "node", None)
    if not node:
        raise Refusal("--node is required")
    if node not in RECIPES:
        raise Refusal(
            "unknown node %r; this file knows %s" % (node, ", ".join(sorted(RECIPES)))
        )

    ctx = LegContext(
        node=node,
        job=getattr(args, "job", None) or "",
        lock=lock,
        lock_hash8=hash8,
        source=source,
        checkout=getattr(args, "checkout", None) or _default_checkout(),
        runner_temp=getattr(args, "runner_temp", None) or _default_runner_temp(),
        scope=resolve_scope(node, getattr(args, "edges", None)),
    )
    if source == "PR body":
        log("chain: lock from PR body")
    else:
        log("chain: lock from %s" % source)
    log("chain: lock %s node %s" % (ctx.lock_hash8, ctx.node))
    # A twin belongs to ONE job: selected anywhere else it would clone the twin and record a
    # `<workflow> twin` identity row in a job that never drives it.
    for twin in scoped_twins(ctx):
        if not twin_in_job(ctx, twin):
            raise Refusal(
                "--edges %s: the twin is declared for job %r of %s, not --job %r; pass the "
                "selector only in that job" % (twin_selector(twin), twin["job"], twin["workflow"], ctx.job)
            )
    assert_head_sha(ctx)
    return ctx


def observed_head_sha():
    """The sha this run is evidence about.

    On `workflow_dispatch` that is `GITHUB_SHA`. On the train's `pull_request` it is the PR
    HEAD — `GITHUB_SHA` there is the ephemeral merge commit, which is in no lock.
    """
    payload = _event_payload()
    head = ((payload.get("pull_request") or {}).get("head") or {}).get("sha")
    if head and os.environ.get("GITHUB_EVENT_NAME") == "pull_request":
        return str(head)
    return os.environ.get("GITHUB_SHA") or ""


def assert_head_sha(ctx):
    """§C6 — a push between lock and dispatch costs a wasted leg, never a wrong verdict (A20)."""
    expected = ctx.sha(ctx.node)
    if not expected:
        raise Refusal(
            "the lock has no sha for node %r (state %r): a leg cannot be evidence for a node "
            "the lock did not resolve" % (ctx.node, ctx.node_entry(ctx.node).get("state"))
        )
    observed = observed_head_sha()
    if observed != expected:
        raise Refusal(
            "sha mismatch for %s: this run is at %s, the lock says %s — re-lock and "
            "re-dispatch (C6/A20)" % (ctx.node, observed or "(unset)", expected)
        )
    log("chain: sha ok %s == lock.nodes.%s.sha" % (expected, ctx.node))


# ----------------------------------------------------------------------
# the build plan
# ----------------------------------------------------------------------


def overridden_edges(ctx):
    """The node's own `consumes:` entries whose producer is IN this lock with a ws_version."""
    out = []
    for edge in RECIPES[ctx.node]["consumes"]:
        if edge["mode"] == "project":
            continue  # a ProjectReference cannot resolve to a released package by accident
        if ctx.ws_version(edge["node"]):
            out.append(edge)
    return out


def consumer_pins(edge):
    """The pins of `edge` that are CONSUMER PROJECTS a job can select one by one.

    Only a `nuget` pin is a project whose own `project.assets.json` is the evidence (§C9). The
    pins of every other mode are version files (`NuGetConfig.cs`, `package.json`, a `.ts`
    constant), never a thing a job restores, so they are not selectors.
    """
    return list(edge.get("pins") or ()) if edge["mode"] == "nuget" else []


def scope_selectors(node_id):
    """Every `--edges` value `node_id` accepts: modes, producers, refs, consumer pins, `self`,
    `twin:<node>`, `none`."""
    edges = [edge for edge in RECIPES[node_id]["consumes"] if edge["mode"] != "project"]
    out = sorted({edge["mode"] for edge in edges}) + sorted({edge["node"] for edge in edges})
    for value in [edge["ref"] for edge in edges] + sorted({p for edge in edges for p in consumer_pins(edge)}):
        # one ref can back two modes (unity-mcp consumes McpPlugin as DLL drops AND as a NuGet
        # package for its engine-free project) — offer each selector once
        if value not in out:
            out.append(value)
    if is_root_node(node_id) and own_packable_artifacts(node_id):
        out.append(SCOPE_SELF)
    for twin in node_twins(node_id):
        if twin_selector(twin) not in out:
            out.append(twin_selector(twin))
    out.append(SCOPE_NONE)
    return out


def resolve_scope(node_id, raw_values):
    """`--edges` -> `None` (no filter: every edge is in scope) or a frozenset of selectors.

    An unknown selector is a refusal, never an empty scope: a typo that silently scoped a job to
    nothing would turn every identity proof into a `skipped[]` row and the leg GREEN.
    """
    values = []
    for raw in raw_values or ():
        for part in str(raw).split(","):
            part = part.strip()
            if part and part not in values:
                values.append(part)
    if not values:
        return None
    valid = scope_selectors(node_id)
    unknown = [value for value in values if value not in valid]
    if unknown:
        raise Refusal(
            "--edges %s: not a selector node %s accepts (valid: %s)"
            % (", ".join(unknown), node_id, ", ".join(valid))
        )
    if SCOPE_NONE in values and len(values) > 1:
        raise Refusal("--edges none cannot be combined with other selectors (got %s)" % ", ".join(values))
    return frozenset(values)


def scope_label(scope):
    return "(all)" if scope is None else ",".join(sorted(scope))


def scope_json(scope):
    """The scope as `leg-state.json` stores it: `null` for no filter, else a sorted list."""
    return None if scope is None else sorted(scope)


def _edge_selected_whole(edge, scope):
    return scope is None or edge["mode"] in scope or edge["node"] in scope or edge["ref"] in scope


def edge_in_scope(edge, scope):
    """An edge is in scope when a mode / producer / ref selects it, or any of its consumer pins does."""
    return _edge_selected_whole(edge, scope) or any(pin in scope for pin in consumer_pins(edge))


def pins_in_scope(edge, scope):
    """The pins of an IN-SCOPE edge this job proves.

    A mode / producer / ref selector takes every pin; a pin selector takes only the consumer
    projects it names. That is what lets a job that restores SOME of an edge's projects (Godot's
    `dotnet-build-test` never restores `Godot-Tests.csproj`, which is not in the sln) prove the
    ones it does restore without a missing assets file for the others turning it RED.
    """
    pins = list(edge.get("pins") or ())
    return pins if _edge_selected_whole(edge, scope) else [p for p in pins if p in scope]


def pins_out_of_scope(edge, scope):
    """The pins of an IN-SCOPE edge this job leaves unproven (a named `skipped[]` row each)."""
    proven = pins_in_scope(edge, scope)
    return [p for p in (edge.get("pins") or ()) if p not in proven]


def scoped_edges(ctx):
    """`(in scope, out of scope)` over `overridden_edges(ctx)`, per this job's `--edges`."""
    inside, outside = [], []
    for edge in overridden_edges(ctx):
        (inside if edge_in_scope(edge, ctx.scope) else outside).append(edge)
    return inside, outside


def own_packable_artifacts(node_id):
    """The nupkg / tgz artifacts a node's OWN `pack:` produces, by artifact key."""
    return [
        (key, body) for key, body in sorted(RECIPES[node_id]["artifacts"].items())
        if body.get("kind") in ("nupkg", "tgz") and not body.get("pending")
    ]


def is_root_node(node_id):
    """A ROOT consumes nothing in RECIPES (§C4 "root nodes" — ReflectorNet, cli-core).

    Decided by the RECIPE, never by the lock: a consumer whose producers all happen to be
    `released` in one lock has no overridden edge either, yet it is not a root — its workflow
    never packs its own artifact into the chain feed, so treating it as one would demand a proof
    no job of it can produce.
    """
    return not any(edge["mode"] != "project" for edge in RECIPES[node_id]["consumes"])


def leg_is_active(ctx):
    """A leg is evidence for this lock when it consumes a ws producer, is a root at a ws version,
    OR this job selected a twin the lock pins at a sha (`--edges twin:<node>`).

    A root builds at `CHAIN_BUILD_PROPS` and packs its own artifact at the lock's ws version for
    its consumers, so it is a leg exactly like any other. A pinned twin is a commit the job must
    prove it ran against. Every other node with no overridden edge is a no-op, as before.
    """
    return (
        bool(overridden_edges(ctx))
        or (is_root_node(ctx.node) and bool(ctx.ws_version(ctx.node)))
        or bool(pinned_twins(ctx))
    )


def own_pack_in_scope(ctx):
    """`(proven, skipped)` own-pack artifacts — a root at a ws version only.

    With no `--edges`, or `--edges self`, the root proves its own pack; any other scope records
    the own pack as a named skip rather than dropping it silently (A22).
    """
    if not (is_root_node(ctx.node) and ctx.ws_version(ctx.node)):
        return [], []
    artifacts = own_packable_artifacts(ctx.node)
    if ctx.scope is None or SCOPE_SELF in ctx.scope:
        return artifacts, []
    return [], artifacts


def scope_skips(ctx, inside, outside, own_outside):
    """One `skipped[]` row per edge, consumer pin and own artifact this job's scope left unproven."""
    reason = "out of this job's --edges scope (%s): not built, not overridden, not proven" % scope_label(ctx.scope)
    rows = [{"step": "identity %s (%s)" % (edge["ref"], edge["mode"]), "reason": reason} for edge in outside]
    pin_reason = "consumer out of this job's --edges scope (%s): not proven" % scope_label(ctx.scope)
    for edge in inside:
        for pin in pins_out_of_scope(edge, ctx.scope):
            rows.append({"step": "identity %s @ %s (%s)" % (edge["ref"], pin, edge["mode"]), "reason": pin_reason})
    rows += [
        {"step": "identity %s/%s (own pack)" % (ctx.node, key), "reason": reason}
        for key, _ in own_outside
    ]
    # A twin is skipped by name only in the job it is declared for: every OTHER job of the node
    # never had it, so a row there would claim a gap that does not exist.
    selected = scoped_twins(ctx)
    rows += [
        {"step": "identity %s (%s)" % (twin_consumer(twin), twin_selector(twin)),
         "reason": "twin not selected: this job passes no --edges %s, so it keeps its own ref and "
                   "nothing is proven" % twin_selector(twin)}
        for twin in node_twins(ctx.node)
        if twin not in selected and ctx.job and ctx.job == twin["job"]
    ]
    return rows


def own_build_props(ctx):
    """`CHAIN_BUILD_PROPS` — `-p:Version=<ws>` when this node's own build takes a version."""
    recipe_build = RECIPES[ctx.node]["build"]
    own_ws = ctx.ws_version(ctx.node)
    if recipe_build and "{ws_version}" in recipe_build and own_ws:
        return "-p:Version=%s" % own_ws
    return None


def own_ws_version_export(ctx):
    """`CHAIN_WS_VERSION` — a ROOT's own ws version, for whatever ecosystem it packs in.

    `CHAIN_BUILD_PROPS` exists only when the recipe's build takes `{ws_version}` (an MSBuild
    property), so an npm root such as cli-core exported nothing its pack step could stamp —
    yet its workflow packs with `npm pkg set version=$CHAIN_WS_VERSION && npm pack`.
    Exported for every root at a ws version; a consumer's ws version lives in the lock.
    """
    own_ws = ctx.ws_version(ctx.node)
    if is_root_node(ctx.node) and own_ws:
        return own_ws
    return None


def feed_artifact_path(ctx, node_id, body):
    """Where a nupkg / tgz artifact of `node_id` lands in this checkout's feed at its ws version."""
    ws_version = ctx.ws_version(node_id)
    if body["kind"] == "nupkg":
        return ctx.feed_nuget / ("%s.%s.nupkg" % (body["id"], ws_version))
    return ctx.feed_npm / tarball_filename(body["id"], ws_version)


def lower_closure(ctx, edges):
    """Every lower node this leg must self-build, transitively, in topological order."""
    wanted = set()
    stack = [edge["node"] for edge in edges]
    while stack:
        node_id = stack.pop()
        if node_id in wanted or node_id == ctx.node:
            continue
        if not ctx.ws_version(node_id):
            continue
        wanted.add(node_id)
        for edge in RECIPES[node_id]["consumes"]:
            if edge["mode"] != "project" and ctx.ws_version(edge["node"]):
                stack.append(edge["node"])
    return topological_order(wanted)


def placeholder_values(ctx, node_id, clone_dir):
    ws_version = ctx.ws_version(node_id) or ""
    return {
        "ws_version": ws_version,
        "feed": as_posix_abs(ctx.feed_nuget),
        "npm_feed": as_posix_abs(ctx.feed_npm),
        "server_out": as_posix_abs(ctx.feed_server / ws_version) if ws_version else "",
        "node_dir": as_posix_abs(clone_dir),
        "slot": as_posix_abs(ctx.checkout),
        "rid": host_rid(),
    }


def clone_argv(node_id, sha, clone_dir, slug):
    """The PUBLIC-repo, sha-pinned clone of §C4 — four fixed argv lists, never a shell string.

    Every emitted path is ABSOLUTE with forward slashes (`02` §8) so the printed plan can be
    pasted into a shell or a PR body unchanged on any platform.
    """
    url = "https://github.com/%s.git" % slug
    target = as_posix_abs(clone_dir)
    return [
        ["git", "init", "--quiet", target],
        ["git", "-C", target, "remote", "add", "origin", url],
        ["git", "-C", target, "fetch", "--depth", "1", "--quiet", "origin", sha],
        ["git", "-C", target, "checkout", "--quiet", "FETCH_HEAD"],
    ]


def build_plan(ctx, edges):
    """The full clone/build/pack plan, computed with ZERO side effects (`dry-run` prints it)."""
    plan = []
    for node_id in lower_closure(ctx, edges):
        recipe = RECIPES[node_id]
        clone_dir = ctx.clone_dir(node_id)
        values = placeholder_values(ctx, node_id, clone_dir)
        entry = {
            "node": node_id,
            "sha": ctx.sha(node_id),
            "ws_version": ctx.ws_version(node_id),
            "dir": as_posix_abs(clone_dir),
            "clone": clone_argv(node_id, ctx.sha(node_id) or "", clone_dir, recipe["slug"]),
            "override": nuget_rows_for_node(ctx, node_id),
            "npm": npm_lock_edges(ctx, node_id),
            # these strings run with `shell=True`: every pasted value goes through `shell_value`
            "build": substitute(recipe["build"], values, quote=shell_value) if recipe["build"] else None,
            "pack": [substitute(raw, values, quote=shell_value) for raw in recipe["pack"]],
            "expect": expected_artifacts(ctx, node_id),
        }
        plan.append(entry)
    return plan


def expected_artifacts(ctx, node_id):
    """The files a node's `pack:` must have produced. A missing one is a named refusal.

    The exact `[range]` in `Directory.Build.targets` would fail the downstream restore hard
    (NU1102) anyway — this only makes the failure name the missing artifact instead of a
    restore error 40 minutes later.
    """
    ws_version = ctx.ws_version(node_id)
    out = []
    if not ws_version:
        return out
    for key, body in sorted(RECIPES[node_id]["artifacts"].items()):
        if body.get("pending"):
            continue
        kind = body.get("kind")
        if kind in ("nupkg", "tgz"):
            out.append(as_posix_abs(feed_artifact_path(ctx, node_id, body)))
        elif kind == "server-binary":
            rid = host_rid()
            out.append(as_posix_abs(server_exe_path(ctx, body["id"], ws_version, rid)))
    return out


def server_exe_path(ctx, artifact_id, ws_version, rid):
    name = ("%s.exe" % artifact_id) if rid.startswith("win") else artifact_id
    return ctx.feed_server / ws_version / rid / name


def tarball_filename(package_id, ws_version):
    """`@scope/name` + ws -> `scope-name-<ws>.tgz` (npm's own pack naming).

    From the manifest's artifact `id`, NEVER from the consumer's pin: the only tarball this
    file will install is the one the lock names.
    """
    slug = package_id[1:] if package_id.startswith("@") else package_id
    return "%s-%s.tgz" % (slug.replace("/", "-"), ws_version)


# ----------------------------------------------------------------------
# twins — a checkout one job drives beside its own, pinned by the lock
# ----------------------------------------------------------------------


def node_twins(node_id):
    return list(RECIPES[node_id].get("twins") or ())


def twin_selector(twin):
    return SCOPE_TWIN_PREFIX + twin["node"]


def twin_env(twin):
    """`mcp-plugin-dotnet` -> `CHAIN_TWIN_MCP_PLUGIN_DOTNET`."""
    return TWIN_ENV_PREFIX + re.sub(r"[^A-Za-z0-9]+", "_", twin["node"]).upper()


def twin_consumer(twin):
    """The identity row's `consumer`: `concurrency-suite.yml` -> `concurrency-suite twin`."""
    workflow = twin["workflow"]
    stem = workflow.rsplit(".", 1)[0] if workflow.endswith((".yml", ".yaml")) else workflow
    return "%s twin" % stem


def twin_slug(twin):
    return RECIPES[twin["node"]]["slug"]


def twin_repo(twin):
    """The twin's repo NAME (`MCP-Plugin-dotnet`), as the dry-run line prints it."""
    return twin_slug(twin).split("/")[-1]


def twin_in_job(ctx, twin):
    return bool(ctx.job) and ctx.job == twin["job"]


def scoped_twins(ctx):
    """The declared twins this job selected with `--edges twin:<node>` (never implied)."""
    if ctx.scope is None:
        return []
    return [twin for twin in node_twins(ctx.node) if twin_selector(twin) in ctx.scope]


def twin_unpinned(ctx, twin):
    """The lock RELEASES the twin: `lock.resolve` gives a `released` node a version and no sha, so
    there is no commit to clone — the job keeps its own ref and the gap is a named skip."""
    return ctx.node_entry(twin["node"]).get("state") == "released" and not ctx.sha(twin["node"])


def pinned_twins(ctx):
    """The selected twins the lock pins (or claims to): everything `apply` must clone and `record` prove."""
    return [twin for twin in scoped_twins(ctx) if not twin_unpinned(ctx, twin)]


def twin_sha(ctx, twin):
    """The lock's sha for the twin, or a refusal: a twin that is not pinned is not evidence."""
    sha = ctx.sha(twin["node"])
    if not sha:
        raise Refusal(
            "--edges %s: the lock has no sha for node %r (state %r), so the twin cannot be "
            "cloned at a pinned commit" % (twin_selector(twin), twin["node"], ctx.node_entry(twin["node"]).get("state"))
        )
    return sha


def twin_plan(ctx):
    """Every pinned twin as `{twin, sha, dir, env, clone}`, with ZERO side effects: `apply` runs
    exactly the clone `dry-run` prints. A twin whose state claims a sha it lacks is refused here."""
    plan = []
    for twin in pinned_twins(ctx):
        sha = twin_sha(ctx, twin)
        directory = ctx.twin_dir(twin["node"])
        plan.append({
            "twin": twin, "sha": sha, "dir": as_posix_abs(directory), "env": twin_env(twin),
            "clone": clone_argv(twin["node"], sha, directory, twin_slug(twin)),
        })
    return plan


def twin_skips(ctx):
    """One `skipped[]` row per selected twin the lock releases (no sha, nothing cloned or proven)."""
    return [
        {"step": "identity %s (%s)" % (twin_consumer(twin), twin_selector(twin)),
         "reason": "lock.nodes.%s is released (%s) with no sha: nothing cloned, the job used its "
                   "own ref, not proven" % (twin["node"], ctx.node_entry(twin["node"]).get("version"))}
        for twin in scoped_twins(ctx) if twin_unpinned(ctx, twin)
    ]


def git_head(directory):
    """`(sha, None)` from `git -C <directory> rev-parse HEAD`, or `(None, why)`.

    The status comes from `returncode`, and the output is CAPTURED, never piped.
    """
    git = shutil.which("git")
    if git is None:
        return None, "git is not on PATH"
    try:
        completed = subprocess.run(
            [git, "-C", as_posix_abs(directory), "rev-parse", "HEAD"],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=dict(os.environ, **_GIT_ENV),
        )
    except OSError as exc:
        return None, str(exc)
    if completed.returncode != 0:
        detail = completed.stderr.decode("utf-8", "replace").strip()
        return None, "exit %d: %s" % (completed.returncode, detail[:200])
    sha = completed.stdout.decode("utf-8", "replace").strip()
    return (sha, None) if sha else (None, "empty output")


def _twin_proofs(ctx):
    """One identity row per pinned twin: its checkout's HEAD vs the lock's sha.

    A twin dir with no `.git` is `ok: false` BEFORE git runs — `git -C` on a plain directory
    walks UP to any enclosing repo and would report that repo's HEAD instead of failing.
    """
    proofs = []
    for twin in pinned_twins(ctx):
        consumer = twin_consumer(twin)
        package = twin_slug(twin)
        expected = ctx.sha(twin["node"]) or None
        directory = ctx.twin_dir(twin["node"])
        label = "git -C %s rev-parse HEAD" % as_posix_abs(directory)
        if not expected:
            proofs.append(_proof(
                consumer, "lock.nodes.%s has no sha: the twin is not pinned" % twin["node"],
                package, None, None, False,
            ))
            continue
        if not (directory / ".git").exists():
            proofs.append(_proof(
                consumer, "twin dir %s is missing or not a git checkout (apply never cloned it)"
                % as_posix_abs(directory), package, None, expected, False,
            ))
            continue
        resolved, why = git_head(directory)
        if resolved is None:
            proofs.append(_proof(consumer, "%s failed: %s" % (label, why), package, None, expected, False))
            continue
        proofs.append(_proof(consumer, label, package, resolved, expected, resolved == expected))
    return proofs


# ----------------------------------------------------------------------
# the command runner
# ----------------------------------------------------------------------


def run(argv, cwd=None, env=None, shell=False):
    """One command; the exit status comes from `returncode`, never through a pipe.

    A LIST argv has its program resolved through `shutil.which` first. On Windows
    `CreateProcess` appends only `.exe` and ignores `PATHEXT`, so a bare `["npm", ...]` cannot
    find the `npm.cmd` a self-hosted runner actually has — measured here: the bare list raises
    `WinError 2` while the resolved full path runs. `which` is also what `require_tool` reports
    on, so the two can never disagree about which program a leg is about to run.
    """
    printable = argv if isinstance(argv, str) else " ".join(str(a) for a in argv)
    log("$ (%s) %s" % (str(cwd or os.getcwd()).replace("\\", "/"), printable))
    if not shell and not isinstance(argv, str):
        argv = list(argv)
        resolved = shutil.which(str(argv[0]))
        if resolved is None:
            raise Refusal("%r is not on PATH" % str(argv[0]))
        argv[0] = resolved
    completed = subprocess.run(  # noqa: S602 - manifest recipes carry `&&` and `cd`
        argv,
        cwd=str(cwd) if cwd else None,
        env=env,
        shell=shell,
    )
    return completed.returncode


def require_tool(name, why):
    """Refuse with a NAMED error rather than letting a missing SDK read as a build failure.

    The Unreal runner account cannot run `setup-dotnet`/`setup-python` (`test_pull_request.yml`
    `:351-358`), so the leg uses the SYSTEM SDK and says so when there is none.
    """
    if shutil.which(name) is None:
        raise Refusal(
            "%r is not on PATH: %s. This leg never calls setup-*; a self-hosted runner must "
            "carry the system SDK." % (name, why)
        )


def _tool_for(command):
    head = command.strip().split()
    if not head:
        return None
    first = head[0]
    if first == "cd":
        for token in head:
            if token in ("dotnet", "npm", "pnpm"):
                return token
        return None
    return first if first in ("dotnet", "npm", "pnpm") else None


# ----------------------------------------------------------------------
# override: nuget (parity with overrides/nuget.py)
# ----------------------------------------------------------------------


def _check_version(value, what):
    if not value:
        raise Refusal("%s has no ws_version in this lock" % what)
    if not _VERSION_RE.match(value):
        raise Refusal(
            "%s ws_version %r is not a plain NuGet version; an override row must never carry "
            "range syntax, a wildcard or whitespace" % (what, value)
        )
    return value


def nuget_rows_for(ctx, edges):
    """One row per DISTINCT nupkg artifact of every producer the edges name.

    Sibling artifacts of the same producer are included (`McpPlugin.Common` has no `consumes:`
    entry of its own, yet a consumer may reference it directly and the producer packs it at the
    same ws version; an `Update` for an unreferenced package is an MSBuild no-op). Ordered
    topologically by producer, then by package id — byte-stable across runs.
    """
    versions = {}
    for edge in edges:
        if edge["mode"] != "nuget":
            continue
        node_id = edge["node"]
        version = _check_version(ctx.ws_version(node_id), edge["ref"])
        previous = versions.setdefault(node_id, version)
        if previous != version:
            raise Refusal(
                "node %s appears with two ws_versions (%s and %s)" % (node_id, previous, version)
            )
    rows = {}
    for node_id, version in versions.items():
        for artifact_key, body in RECIPES[node_id]["artifacts"].items():
            if body.get("kind") != "nupkg":
                continue
            package_id = body.get("id")
            if not package_id:
                raise Refusal("artifact %s/%s has no `id`" % (node_id, artifact_key))
            existing = rows.get(package_id)
            if existing is not None and existing["ws_version"] != version:
                raise Refusal(
                    "package %s would be pinned to both %s and %s"
                    % (package_id, existing["ws_version"], version)
                )
            rows[package_id] = {
                "package_id": package_id,
                "ws_version": version,
                "node": node_id,
                "artifact": artifact_key,
            }
    order = {nid: i for i, nid in enumerate(topological_order(versions))}
    return sorted(rows.values(), key=lambda r: (order.get(r["node"], 0), r["package_id"]))


def nuget_rows_for_node(ctx, node_id):
    """The rows a node's OWN checkout/clone needs — used by the plan and by the lower builds."""
    edges = [
        edge for edge in RECIPES[node_id]["consumes"]
        if edge["mode"] == "nuget" and ctx.ws_version(edge["node"])
    ]
    return nuget_rows_for(ctx, edges) if edges else []


def render_nuget_config(feed, lock_hash8="nolock"):
    """`<clear/>` + the workspace feed (ABSOLUTE) + nuget.org, in that order."""
    value = str(feed)
    if not Path(value).is_absolute():
        raise Refusal(
            "the workspace NuGet feed must be an ABSOLUTE path; %r is relative and NuGet "
            "resolves it against each process's cwd (B12)" % value
        )
    value = as_posix_abs(Path(value))
    return (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<!-- %s for lock %s. Slot-local; never commit. -->\n"
        "<configuration>\n"
        "  <packageSources>\n"
        "    <clear />\n"
        '    <add key="%s" value="%s" />\n'
        '    <add key="nuget.org" value="%s" protocolVersion="3" />\n'
        "  </packageSources>\n"
        "</configuration>\n" % (GENERATED_MARKER, lock_hash8, NUGET_SOURCE_KEY, value, NUGET_ORG_URL)
    )


def render_targets(rows, lock_hash8="nolock"):
    """The `02` §5 fragment: NU1603/NU1605 promoted, plus one EXACT `[range]` row per package.

    The brackets are the whole point. `Version="5.4.0-ws.g1dff5501"` is a FLOOR, and by SemVer
    §11 nuget.org's stable `5.4.0` satisfies it — a feed missing the ws package would restore
    the RELEASED bits with only a NU1603 warning and score GREEN against the wrong artifact.
    """
    if not rows:
        raise Refusal("refusing to write an override with no rows")
    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "<!-- %s for lock %s. Slot-local; never commit. -->" % (GENERATED_MARKER, lock_hash8),
        "<Project>",
        "  <PropertyGroup Condition=\"'$(%s)' == 'true'\">" % NUGET_PROPERTY,
        "    <WarningsAsErrors>$(WarningsAsErrors);NU1603;NU1605</WarningsAsErrors>",
        "  </PropertyGroup>",
        "  <ItemGroup Condition=\"'$(%s)' == 'true'\">" % NUGET_PROPERTY,
    ]
    for row in rows:
        lines.append(
            '    <PackageReference Update="%s" Version="[%s]" />' % (row["package_id"], row["ws_version"])
        )
    lines += ["  </ItemGroup>", "</Project>", ""]
    return "\n".join(lines)


def _is_ours(path):
    try:
        with io.open(str(path), "r", encoding="utf-8", errors="replace") as handle:
            return GENERATED_MARKER in handle.read(4096)
    except OSError:
        return False


def _refuse_foreign(path):
    if Path(path).exists() and not _is_ours(path):
        raise Refusal(
            "%s already exists and was not written by the chain; refusing to overwrite a file "
            "the repo owns" % as_posix_abs(path)
        )


def write_nuget_override(ctx, root, rows):
    """The two generated files at `root`. All or nothing: both checked before either is written."""
    if not rows:
        return []
    config_text = render_nuget_config(ctx.feed_nuget, ctx.lock_hash8)
    targets_text = render_targets(rows, ctx.lock_hash8)
    for name in (NUGET_CONFIG_NAME, TARGETS_NAME):
        _refuse_foreign(Path(root) / name)
    write_text(Path(root) / NUGET_CONFIG_NAME, config_text, newline="\n")
    write_text(Path(root) / TARGETS_NAME, targets_text, newline="\n")
    return [
        as_posix_abs(Path(root) / NUGET_CONFIG_NAME),
        as_posix_abs(Path(root) / TARGETS_NAME),
    ]


# ----------------------------------------------------------------------
# override: unity-dll (parity with overrides/unity_dll.py)
# ----------------------------------------------------------------------


def unity_dll_name(package_id):
    if not package_id or not package_id.startswith(UNITY_PACKAGE_PREFIX):
        raise Refusal(
            "artifact id %r does not start with %r; cannot derive a flat-drop DLL filename"
            % (package_id, UNITY_PACKAGE_PREFIX)
        )
    return package_id[len(UNITY_PACKAGE_PREFIX):] + ".dll"


def find_nupkg(feed, package_id, ws_version):
    if not Path(feed).is_dir():
        return None
    wanted = ("%s.%s" % (package_id, ws_version)).lower()
    for candidate in sorted(Path(feed).glob("*.nupkg")):
        if candidate.name.lower().startswith(wanted):
            return candidate
    return None


def extract_lib_dll(nupkg, dll_name):
    """`lib/netstandard2.1/<dll>` out of the nupkg zip — the artifact, never a second build."""
    member = UNITY_LIB_ENTRY.format(name=dll_name)
    with zipfile.ZipFile(str(nupkg)) as archive:
        try:
            return archive.read(member)
        except KeyError:
            raise Refusal(
                "%s: no %r entry (Unity needs the netstandard2.1 build); has: %s"
                % (nupkg, member, ", ".join(sorted(archive.namelist())))
            )


def apply_unity_dll(ctx, edges):
    """Six flat DLL drops. `.nuget-installed.json` and every `.dll.meta` stay untouched.

    Rewriting the manifest to the ws version makes the in-editor resolver treat the package as
    "not installed", re-download the RELEASED nupkg and DELETE the DLLs this just dropped
    (review A2); a `.dll.meta` carries a per-project GUID (A 2b).
    """
    dll_map = {}
    for edge in edges:
        package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
        dll_map[unity_dll_name(package_id)] = (edge, package_id)
    drops = []
    for edge in edges:
        for drop in edge.get("drops") or ():
            if drop not in drops:
                drops.append(drop)
    if not drops:
        raise Refusal("unity-dll: no `drops:` declared on any edge for this node")

    # Validate EVERY drop against the CURRENT tree before touching anything.
    problems = []
    for drop in drops:
        drop_dir = ctx.checkout / drop
        if not drop_dir.is_dir():
            problems.append("%s: directory does not exist" % drop)
            continue
        for dll_name in dll_map:
            if not (drop_dir / dll_name).is_file():
                problems.append("%s/%s: missing" % (drop, dll_name))
            if not (drop_dir / (dll_name + ".meta")).is_file():
                problems.append("%s/%s.meta: missing" % (drop, dll_name))
    if problems:
        raise Refusal("unity-dll apply refused (nothing written): " + "; ".join(problems))

    extracted = {}
    for dll_name, (edge, package_id) in sorted(dll_map.items()):
        ws_version = ctx.ws_version(edge["node"])
        nupkg = find_nupkg(ctx.feed_nuget, package_id, ws_version)
        if nupkg is None:
            raise Refusal(
                "%s: no .nupkg for %s %s under %s — pack it before override"
                % (edge["ref"], package_id, ws_version, as_posix_abs(ctx.feed_nuget))
            )
        data = extract_lib_dll(nupkg, dll_name)
        out_dir = ctx.feed_unity_dll / ws_version
        out_dir.mkdir(parents=True, exist_ok=True)
        (out_dir / dll_name).write_bytes(data)
        extracted[dll_name] = data
        log("unity-dll: extracted %s (%s) from %s" % (dll_name, ws_version, nupkg.name))

    written = []
    for drop in drops:
        drop_dir = ctx.checkout / drop
        for dll_name, data in sorted(extracted.items()):
            target = drop_dir / dll_name
            if target.read_bytes() != data:
                target.write_bytes(data)
                written.append(as_posix_abs(target))
    log("unity-dll: %d file(s) rewritten across %d drop(s)" % (len(written), len(drops)))
    return written


# ----------------------------------------------------------------------
# override: pnpm (parity with overrides/pnpm.py)
# ----------------------------------------------------------------------


def _eol(text):
    return "\r\n" if text.count("\r\n") > text.count("\n") - text.count("\r\n") else "\n"


def _strip_generated_block(text):
    if PNPM_BEGIN not in text:
        return text
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    out = []
    skipping = False
    for line in normalized.split("\n"):
        if line.strip() == PNPM_BEGIN:
            skipping = True
            continue
        if skipping:
            if line.strip() == PNPM_END:
                skipping = False
            continue
        out.append(line)
    eol = _eol(text)
    return eol.join(out).rstrip("\r\n") + (eol if out else "")


def compose_workspace_yaml(existing, entries):
    """Append (or replace) the generated `overrides:` block, keeping `packages:` untouched."""
    eol = _eol(existing) if existing else "\n"
    stripped = _strip_generated_block(existing)
    if re.search(r"^overrides\s*:", stripped.replace("\r\n", "\n"), re.MULTILINE):
        raise Refusal(
            "pnpm-workspace.yaml already declares a top-level `overrides:` the chain did not "
            "write — refusing to append a second one"
        )
    body = stripped.rstrip("\r\n")
    lines = [PNPM_BEGIN, "overrides:"]
    for package_id, spec in entries:
        lines.append('  "%s": "%s"' % (package_id, spec))
    lines.append(PNPM_END)
    block = eol.join(lines)
    prefix = (body + eol + eol) if body else ""
    return prefix + block + eol


def rekey_package_json_text(raw, pkg, old_ver, new_ver):
    old_token = "%s@%s" % (pkg, old_ver)
    new_token = "%s@%s" % (pkg, new_ver)
    return raw.replace(old_token, new_token), raw.count(old_token)


def drop_patch_key_text(raw, key):
    needle = '"%s"' % key
    lines = raw.split("\n")
    hits = [i for i, line in enumerate(lines) if needle in line and ":" in line]
    if len(hits) != 1:
        return raw, False
    index = hits[0]
    del lines[index]
    previous = index - 1
    while previous >= 0 and not lines[previous].strip():
        previous -= 1
    following = index
    while following < len(lines) and not lines[following].strip():
        following += 1
    if 0 <= previous < len(lines) and following < len(lines):
        body = lines[previous].rstrip()
        eol = lines[previous][len(body):]
        if body.endswith(",") and lines[following].strip().startswith(("}", "]")):
            lines[previous] = body[:-1] + eol
    return "\n".join(lines), True


def git_blob_sha1(data):
    header = b"blob " + str(len(data)).encode("ascii") + b"\x00"
    return hashlib.sha1(header + data).hexdigest()  # noqa: S324 - git's own blob hash


def parse_patch(patch_text):
    diff_match = re.search(r"^diff --git a/(.+?) b/(.+)$", patch_text, re.MULTILINE)
    index_match = re.search(r"^index\s+([0-9a-fA-F]+)\.\.([0-9a-fA-F]+)", patch_text, re.MULTILINE)
    return {
        "path": diff_match.group(1) if diff_match else None,
        "from_hash": index_match.group(1) if index_match else None,
        "to_hash": index_match.group(2) if index_match else None,
    }


def read_tarball_member(tgz, in_package_relpath):
    import tarfile

    want = ("package/%s" % in_package_relpath).replace("\\", "/")
    with tarfile.open(str(tgz), "r:gz") as archive:
        try:
            member = archive.getmember(want)
        except KeyError:
            member = None
            for candidate in archive.getmembers():
                if candidate.isfile() and candidate.name.replace("\\", "/").endswith(in_package_relpath):
                    member = candidate
                    break
            if member is None:
                raise KeyError(want)
        extracted = archive.extractfile(member)
        if extracted is None:
            raise KeyError(want)
        return extracted.read()


def _patched_dependencies(app_dir):
    data = json.loads(read_text(Path(app_dir) / "package.json"))
    block = (data.get("pnpm") or {}).get("patchedDependencies") or {}
    if not isinstance(block, dict):
        raise Refusal("package.json: pnpm.patchedDependencies is not an object")
    return block


def rekey_pnpm_patches(ctx, app_dir, plans):
    """Re-key (or DROP) every `patchedDependencies` entry of an overridden package (A21).

    `allowUnusedPatches` defaults to false, so a stale version-exact key is a hard
    `ERR_PNPM_UNUSED_PATCH` — this must run BEFORE the install. A patch whose recorded base
    blob is not the ws tarball's own copy is DROPPED with a loud note, never silently kept and
    never regenerated: only the operator may decide a patch guarding a crash is obsolete.
    """
    notes = []
    package_json = Path(app_dir) / "package.json"
    if not package_json.is_file():
        raise Refusal("no package.json at %s" % as_posix_abs(app_dir))
    patched = _patched_dependencies(app_dir)
    for plan in plans:
        prefix = "%s@" % plan["package_id"]
        keys = [key for key in patched if isinstance(key, str) and key.startswith(prefix)]
        if not keys:
            continue
        if len(keys) > 1:
            raise Refusal(
                "expected at most one `%s*` patchedDependencies entry, found %d: %s"
                % (prefix, len(keys), keys)
            )
        old_key = keys[0]
        old_ver = old_key[len(prefix):]
        new_key = "%s@%s" % (plan["package_id"], plan["ws_version"])
        if old_key == new_key:
            notes.append("patch already keyed to %s (idempotent)" % new_key)
            continue
        old_value = patched[old_key]
        if not isinstance(old_value, str) or not old_value:
            raise Refusal("patchedDependencies[%r] is not a patch-file path: %r" % (old_key, old_value))
        new_value = old_value.replace(old_key, new_key)
        old_patch = Path(app_dir) / old_value
        new_patch = Path(app_dir) / new_value
        source = old_patch if old_patch.is_file() else (new_patch if new_patch.is_file() else None)
        if source is None:
            raise Refusal(
                "patch file not found (looked for %s and %s)"
                % (as_posix_abs(old_patch), as_posix_abs(new_patch))
            )
        info = parse_patch(read_text(source))
        if info["from_hash"] is None:
            raise Refusal("no `index <from>..<to>` line in patch %s" % as_posix_abs(source))
        relpath = info["path"] or "dist/version.js"
        try:
            published = git_blob_sha1(read_tarball_member(plan["tarball"], relpath))
        except KeyError:
            raise Refusal(
                "%s has no `package/%s` member (the patched file) — cannot verify the patch base"
                % (plan["tarball_abs"], relpath)
            )
        raw = read_text(package_json)
        recorded = info["from_hash"]
        matches = bool(recorded) and published[: len(recorded)].lower() == recorded.lower()
        if matches:
            if old_patch.is_file() and not new_patch.exists():
                if run(["git", "mv", old_value, new_value], cwd=app_dir,
                       env=dict(os.environ, **_GIT_ENV)) != 0:
                    raise Refusal("`git mv %s %s` failed in %s" % (old_value, new_value, as_posix_abs(app_dir)))
            updated, count = rekey_package_json_text(raw, plan["package_id"], old_ver, plan["ws_version"])
            if count == 0:
                raise Refusal("expected to re-point `%s` in package.json but found no occurrence" % old_key)
            _assert_only_patch_entry_changed(raw, updated, old_key, new_key, new_value)
            write_text(package_json, updated)
            notes.append(
                "patch re-keyed: %s -> %s (base blob %s matches the ws tarball's %s); patch "
                "file renamed to %s" % (old_key, new_key, recorded, relpath, new_value)
            )
        else:
            updated, removed = drop_patch_key_text(raw, old_key)
            if not removed:
                raise Refusal("could not locate the `%s` line in package.json to drop it" % old_key)
            _assert_only_patch_entry_changed(raw, updated, old_key, None, None)
            write_text(package_json, updated)
            message = (
                "patch dropped: base mismatch (%s vs ws %s) — the patch's recorded base blob %s "
                "is not the ws tarball's %s (%s); the key was removed for this checkout and %s "
                "was left in place. NEVER regenerated here." % (
                    old_key, plan["ws_version"], recorded, relpath,
                    published[: len(recorded)], old_value,
                )
            )
            log(message)
            notes.append(message)
        patched = _patched_dependencies(app_dir)
    return notes


def _assert_only_patch_entry_changed(before_raw, after_raw, old_key, new_key, new_value):
    before = json.loads(before_raw)
    after = json.loads(after_raw)
    patched = before.setdefault("pnpm", {}).setdefault("patchedDependencies", {})
    patched.pop(old_key, None)
    if new_key is not None:
        patched[new_key] = new_value
    if before != after:
        raise Refusal(
            "package.json edit touched more than the `%s` patch entry — refusing" % old_key
        )


# ----------------------------------------------------------------------
# override: npm, BEFORE the consumer's own `npm ci`

#: The dependency maps an npm pin can live in (a CLI declares cli-core under `dependencies`).
NPM_DEP_SECTIONS = ("dependencies", "devDependencies", "optionalDependencies")
#: Regenerates ONLY the lockfile — nothing is installed, no lifecycle script runs.
NPM_RELOCK_ARGV = ["npm", "install", "--package-lock-only", "--ignore-scripts", "--no-audit",
                   "--no-fund"]


def npm_lock_edges(ctx, node_id):
    """`node_id`'s npm edges whose producer is AT A WS VERSION in this lock (so it is packed)."""
    return [e for e in RECIPES[node_id]["consumes"]
            if e["mode"] == "npm" and ctx.ws_version(e["node"])]


def npm_preinstall_plans(ctx, root, edges):
    """One rewrite per npm edge: `<root>/<path>/package.json` pins the ws tarball by `file:`."""
    plans = []
    for edge in edges:
        body = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]
        tarball = feed_artifact_path(ctx, edge["node"], body)
        plans.append({"dir": Path(root) / (edge.get("path") or "."), "package_id": body["id"],
                      "tarball": tarball, "spec": "file:%s" % as_posix_abs(tarball)})
    return plans


def describe_npm_preinstall(plan):
    return "%s: %s -> %s, then %s" % (as_posix_abs(plan["dir"] / "package.json"), plan["package_id"],
                                      plan["spec"], " ".join(NPM_RELOCK_ARGV))


def npm_preinstall(ctx, root, edges):
    """Redirect each npm pin to `file:<ws tgz>` and relock BEFORE anything runs `npm ci` there.

    A train's future range (`^0.5.0`) is on no registry yet, so `npm ci` itself would die ETARGET
    before a post-install swap could run. Rewritten in the leg checkout / a feed clone only; never
    committed. Returns the rewritten paths.
    """
    plans = npm_preinstall_plans(ctx, root, edges)
    if plans:
        require_tool("npm", "the npm pin is relocked onto the ws tarball before `npm ci`")
    written = []
    for plan in plans:
        if not plan["tarball"].is_file():
            raise Refusal("tarball missing for %s -> %s (never falling back to the registry)"
                          % (plan["package_id"], as_posix_abs(plan["tarball"])))
        path = plan["dir"] / "package.json"
        if not path.is_file():
            raise Refusal("npm pin %s does not exist" % as_posix_abs(path))
        try:
            data = json.loads(read_text(path).lstrip("﻿"))
        except ValueError as exc:
            raise Refusal("npm pin %s is not valid JSON: %s" % (as_posix_abs(path), exc))
        if not isinstance(data, dict):
            raise Refusal("npm pin %s is not a JSON object" % as_posix_abs(path))
        sections = [name for name in NPM_DEP_SECTIONS
                    if isinstance(data.get(name), dict) and plan["package_id"] in data[name]]
        if not sections:
            raise Refusal("%s declares no %s dependency to redirect to the ws tarball"
                          % (as_posix_abs(path), plan["package_id"]))
        for name in sections:
            data[name][plan["package_id"]] = plan["spec"]
        write_text(path, json.dumps(data, indent=2, ensure_ascii=False) + "\n")
        if run(NPM_RELOCK_ARGV, cwd=plan["dir"], env=ctx.command_env()) != 0:
            raise Refusal("`%s` failed in %s" % (" ".join(NPM_RELOCK_ARGV), as_posix_abs(plan["dir"])))
        written.extend([as_posix_abs(path), as_posix_abs(plan["dir"] / "package-lock.json")])
        log("chain: npm " + describe_npm_preinstall(plan))
    return written


def pnpm_plans(ctx, edges):
    plans = []
    for edge in edges:
        package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
        ws_version = ctx.ws_version(edge["node"])
        tarball = ctx.feed_npm / tarball_filename(package_id, ws_version)
        plans.append({
            "edge": edge,
            "package_id": package_id,
            "ws_version": ws_version,
            "tarball": tarball,
            "tarball_abs": as_posix_abs(tarball),
            "spec": "file:%s" % as_posix_abs(tarball),
        })
    return sorted(plans, key=lambda p: p["package_id"])


def apply_pnpm(ctx, edges):
    """Re-key the patch, write `overrides:`, then ONE non-frozen `pnpm install` (A21/B5)."""
    plans = pnpm_plans(ctx, edges)
    missing = [p for p in plans if not p["tarball"].is_file()]
    if missing:
        raise Refusal(
            "tarball missing for %d pnpm edge(s) (never falling back to the registry): %s"
            % (len(missing), "; ".join("%s -> %s" % (p["edge"]["ref"], p["tarball_abs"]) for p in missing))
        )
    app_dir = ctx.checkout
    notes = rekey_pnpm_patches(ctx, app_dir, plans)
    workspace_yaml = app_dir / "pnpm-workspace.yaml"
    existing = read_text(workspace_yaml) if workspace_yaml.is_file() else ""
    entries = [(p["package_id"], p["spec"]) for p in plans]
    write_text(workspace_yaml, compose_workspace_yaml(existing, entries))
    notes.extend("pnpm-workspace.yaml overrides: %s -> %s" % (pid, spec) for pid, spec in entries)
    require_tool("pnpm", "the App leg installs its overrides with pnpm")
    # an argv list, not a `shell=True` string: no lock value reaches this command. On Windows, `run`
    # resolves `pnpm.cmd`, and a `.cmd` still executes through cmd.exe.
    if run(PNPM_INSTALL_CMD.split(), cwd=app_dir, env=ctx.command_env()) != 0:
        raise Refusal("`%s` failed in %s" % (PNPM_INSTALL_CMD, as_posix_abs(app_dir)))
    notes.append("%s: %s exit 0" % (as_posix_abs(app_dir), PNPM_INSTALL_CMD))
    return [as_posix_abs(workspace_yaml), as_posix_abs(app_dir / "package.json")], notes


# ----------------------------------------------------------------------
# PE VS_VERSIONINFO (parity with identity.py) — a pure file read, never a process
# ----------------------------------------------------------------------
#
# The `ProductVersion` Roslyn stamps from `-p:Version` is the ONLY identity carrier for the
# single-file server apphost (no `deps.json`; an unrecognised `--version` STARTS a listener on
# the default port — A8, two orphans killed during the research probe). A pure-Python walk of
# the resource directory reads a Windows image from a Linux leg exactly as from the dev box,
# with no `ctypes` and no `win32api`. Ported verbatim from `.scripts/chain/identity.py`; the
# parity test reads the SAME committed PE fixture through both.

RT_VERSION = 16
_IMAGE_DIRECTORY_ENTRY_RESOURCE = 2


class PeFormatError(Exception):
    """The file is not a PE image, or its resource directory is malformed."""


def _u16(buf, off):
    return struct.unpack_from("<H", buf, off)[0]


def _u32(buf, off):
    return struct.unpack_from("<I", buf, off)[0]


def _align4(value):
    return (value + 3) & ~3


def _pe_sections(buf):
    """`([(va, vsize, raw_size, raw_ptr)], resource_rva, resource_size)`."""
    if len(buf) < 0x40 or buf[:2] != b"MZ":
        raise PeFormatError("not a PE image: missing `MZ`")
    pe_off = _u32(buf, 0x3C)
    if len(buf) < pe_off + 24 or buf[pe_off:pe_off + 4] != b"PE\0\0":
        raise PeFormatError("not a PE image: missing `PE\\0\\0` signature")
    n_sections = _u16(buf, pe_off + 6)
    size_opt = _u16(buf, pe_off + 20)
    opt_off = pe_off + 24
    magic = _u16(buf, opt_off)
    if magic == 0x10B:
        dd_off = opt_off + 96
    elif magic == 0x20B:
        dd_off = opt_off + 112
    else:
        raise PeFormatError("unknown optional-header magic 0x%04x" % magic)
    n_dirs = _u32(buf, dd_off - 4)
    if n_dirs <= _IMAGE_DIRECTORY_ENTRY_RESOURCE:
        raise PeFormatError("image has no resource data directory")
    res_rva = _u32(buf, dd_off + 8 * _IMAGE_DIRECTORY_ENTRY_RESOURCE)
    res_size = _u32(buf, dd_off + 8 * _IMAGE_DIRECTORY_ENTRY_RESOURCE + 4)
    sec_off = opt_off + size_opt
    sections = []
    for i in range(n_sections):
        base = sec_off + 40 * i
        if base + 40 > len(buf):
            raise PeFormatError("truncated section table")
        sections.append(
            (_u32(buf, base + 12), _u32(buf, base + 8), _u32(buf, base + 16), _u32(buf, base + 20))
        )
    return sections, res_rva, res_size


def _rva_to_offset(sections, rva):
    for va, vsize, raw_size, raw_ptr in sections:
        span = max(vsize, raw_size)
        if va <= rva < va + span:
            return raw_ptr + (rva - va)
    raise PeFormatError("RVA 0x%08x is in no section" % rva)


def _find_resource(buf, base, wanted_type):
    """Walk type -> name -> language and return the first leaf's `(rva, size)`."""

    def entries(dir_off):
        named = _u16(buf, dir_off + 12)
        by_id = _u16(buf, dir_off + 14)
        for i in range(named + by_id):
            entry = dir_off + 16 + 8 * i
            yield _u32(buf, entry), _u32(buf, entry + 4)

    for name_or_id, offset in entries(base):
        if name_or_id & 0x80000000 or name_or_id != wanted_type:
            continue
        if not offset & 0x80000000:
            return None
        for _, name_offset in entries(base + (offset & 0x7FFFFFFF)):
            if not name_offset & 0x80000000:
                continue
            for _, lang_offset in entries(base + (name_offset & 0x7FFFFFFF)):
                if lang_offset & 0x80000000:
                    continue
                leaf = base + lang_offset
                return _u32(buf, leaf), _u32(buf, leaf + 4)
    return None


def _read_utf16z(buf, off, limit):
    end = off
    while end + 1 < limit and buf[end:end + 2] != b"\x00\x00":
        end += 2
    return buf[off:end].decode("utf-16-le", "replace"), end + 2


def _parse_version_children(buf, off, end):
    out = []
    while off + 6 <= end:
        length = _u16(buf, off)
        if length == 0:
            break
        block_end = min(off + length, end)
        value_length = _u16(buf, off + 2)
        value_type = _u16(buf, off + 4)
        key, after_key = _read_utf16z(buf, off + 6, block_end)
        value_start = _align4(after_key)
        # For a text value wValueLength counts CHARACTERS, for binary BYTES — and toolchains
        # disagree, so the block end is the authority and the declared length only a bound.
        declared = value_length * 2 if value_type == 1 else value_length
        value_end = min(value_start + declared, block_end) if declared else value_start
        value = None
        if value_type == 1 and value_end > value_start:
            value = buf[value_start:value_end].decode("utf-16-le", "replace").rstrip("\x00")
        children_start = _align4(value_end)
        children = (
            _parse_version_children(buf, children_start, block_end)
            if children_start < block_end else []
        )
        out.append({"key": key, "value": value, "children": children})
        off = _align4(block_end)
    return out


def pe_version_strings(path):
    """Every `StringFileInfo` entry of the first RT_VERSION resource. `{}` when absent."""
    return pe_version_strings_from_bytes(Path(path).read_bytes())


def pe_version_strings_from_bytes(buf):
    """`pe_version_strings` over an in-memory image (a DLL read straight out of a nupkg)."""
    sections, res_rva, res_size = _pe_sections(buf)
    if not res_rva or not res_size:
        return {}
    base = _rva_to_offset(sections, res_rva)
    leaf = _find_resource(buf, base, RT_VERSION)
    if leaf is None:
        return {}
    data_rva, data_size = leaf
    data_off = _rva_to_offset(sections, data_rva)
    blocks = _parse_version_children(buf, data_off, min(data_off + data_size, len(buf)))
    out = {}
    for root in blocks:
        if root["key"] != "VS_VERSION_INFO":
            continue
        for child in root["children"]:
            if child["key"] != "StringFileInfo":
                continue
            for table in child["children"]:
                for entry in table["children"]:
                    if entry["value"] is not None:
                        out[entry["key"]] = entry["value"]
    return out


def pe_product_version(path):
    return pe_version_strings(path).get("ProductVersion")


def safe_pe_product_version(path):
    """`pe_product_version`, or `None` on a POSIX apphost (ELF/Mach-O has no PE resources)."""
    try:
        return pe_product_version(path)
    except (PeFormatError, OSError, struct.error):
        return None


# ----------------------------------------------------------------------
# override: server binary (parity with server_binary.py)
# ----------------------------------------------------------------------


def write_server_marker(directory, ws_version, sha, rid, publish_cmd, exe, product_version=None):
    """`<out>/chain-identity.json`, schema EXACTLY `SERVER_MARKER_FIELDS`.

    Written from the publish INPUTS. The exe is never executed to learn its version: a
    single-file publish has no `deps.json` and an unrecognised `--version` STARTS a listener
    on the server's default port (A8).
    """
    payload = {
        "ws_version": ws_version,
        "sha": sha,
        "rid": rid,
        "publish_cmd": publish_cmd,
        "exe_sha256": sha256_file(exe),
        "product_version": (
            product_version if product_version is not None else safe_pe_product_version(exe)
        ),
    }
    target = Path(directory) / SERVER_MARKER_NAME
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(payload, indent=2, sort_keys=False) + "\n", encoding="utf-8")
    return target


def server_env(ctx, edges):
    """`CHAIN_SERVER_PATH` plus every edge's own `env:` name (§C4)."""
    ws_version = ctx.ws_version("gamedev-mcp-server")
    rid = host_rid()
    artifact_id = RECIPES["gamedev-mcp-server"]["artifacts"]["binary"]["id"]
    exe = server_exe_path(ctx, artifact_id, ws_version, rid)
    if not exe.is_file():
        raise Refusal(
            "server binary missing at %s; the gamedev-mcp-server pack step must run first"
            % as_posix_abs(exe)
        )
    exe_str = as_posix_abs(exe)
    env = {"CHAIN_SERVER_PATH": exe_str}
    for edge in edges:
        if edge.get("env"):
            env[edge["env"]] = exe_str
    return env


# ----------------------------------------------------------------------
# subcommands
# ----------------------------------------------------------------------

COMMANDS = {}


def command(name, help_text):
    """Register a subcommand. Independent functions in a table so `p2-replay-leg` can ADD
    `fetch-fixtures` as a purely additive diff (no dispatch `if` to edit)."""

    def decorate(function):
        COMMANDS[name] = (function, help_text)
        return function

    return decorate


_GITHUB_FILE_KEY_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*\Z")


def _checked_github_lines(what, pairs):
    """`KEY=value\\n` lines for `$GITHUB_ENV` / `$GITHUB_OUTPUT` — or a refusal, before ANY is written.

    Both files are line-oriented: a CR or LF inside a value (or a key) starts a NEW `KEY=value`
    line, i.e. it injects a variable of the value's choosing into every later step. The runner
    splits on CR as well as LF, so both are refused, together with NUL and a key that is not an
    identifier. Every pair is checked first, so a refused batch writes nothing at all.
    """
    lines = []
    for key, value in pairs:
        key_text, value_text = str(key), str(value)
        if not _GITHUB_FILE_KEY_RE.match(key_text):
            raise Refusal("refusing to write %s key %r: not an identifier" % (what, key_text))
        if any(ch in value_text for ch in "\r\n\x00"):
            raise Refusal(
                "refusing to write %s value for %s: it carries a CR, LF or NUL (%r), which would "
                "inject extra keys" % (what, key_text, value_text)
            )
        lines.append("%s=%s\n" % (key_text, value_text))
    return lines


def _append_github_file(env_name, pairs):
    path = os.environ.get(env_name)
    if not path:
        return
    lines = _checked_github_lines("$%s" % env_name, pairs)
    # `newline=""`: write the `\n` verbatim; a text-mode `\r\n` on Windows is harmless to the
    # runner but would make the file's bytes depend on the platform that wrote it.
    with io.open(path, "a", encoding="utf-8", newline="") as handle:
        handle.writelines(lines)


def _write_github_env(pairs):
    _append_github_file("GITHUB_ENV", pairs)


def _write_github_output(pairs):
    _append_github_file("GITHUB_OUTPUT", pairs)


#: `leg-state.json` carries whether `apply` got to the end. `record` goes RED for an active leg
#: whose state is missing or not `ok`, so a failed or skipped apply can never record GREEN.
APPLY_STATUS_KEY = "apply"
APPLY_STARTED = "started"
APPLY_OK = "ok"


def apply_status_proofs(ctx, state):
    """An ACTIVE leg is evidence only if this job's `apply` completed.

    A failed `apply` exits 2 but the `if: always()` record step still runs, and every identity
    row can read OK against a checkout the override never reached (a released restore of a
    consumer pinned to the same version, a stale drop). This row is what keeps that RED.
    """
    if not leg_is_active(ctx):
        return []
    status = state.get(APPLY_STATUS_KEY)  # `_load_state` returns `{}` when there is no file
    if status == APPLY_OK:
        return []
    what = "apply did not complete (status %r)" % status if state else "apply never ran in this job"
    return [_proof(ctx.node, "%s: %s" % (ctx.state_path.name, what), "", status, APPLY_OK, False)]


def _save_state(ctx, state):
    ctx.state_path.parent.mkdir(parents=True, exist_ok=True)
    ctx.state_path.write_text(json.dumps(state, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _load_state(ctx):
    if not ctx.state_path.is_file():
        return {}
    try:
        return json.loads(ctx.state_path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


@command("apply", "self-build the lower layers into a local feed and write this node's override")
def cmd_apply(args):
    ctx = build_context(args)
    if ctx is None:
        return EXIT_OK
    started = time.time()
    if not leg_is_active(ctx):
        log("chain: %s consumes nothing inside this lock and is not a root node at a ws version; "
            "nothing to override" % ctx.node)
        _write_github_output([("active", "false")])
        return EXIT_OK
    edges, outside = scoped_edges(ctx)
    if ctx.scope is not None:
        log("chain: --edges %s" % scope_label(ctx.scope))
    for edge in outside:
        log("chain: out of this job's scope: %s (%s) — not built, not overridden" % (edge["ref"], edge["mode"]))
    if is_root_node(ctx.node):
        log("chain: root node: no lower layers; %s packs its own artifact at %s"
            % (ctx.node, ctx.ws_version(ctx.node)))

    for directory in (ctx.feed_nuget, ctx.feed_npm, ctx.feed_server, ctx.feed_unity_dll, ctx.chain_temp):
        directory.mkdir(parents=True, exist_ok=True)
    # Written BEFORE any build: every refusal below leaves `apply: started` behind, which
    # `record` reads as RED. A state saved only on success cannot tell "apply failed" from
    # "apply never ran" from a stale file — and neither may record GREEN.
    _save_state(ctx, {
        "node": ctx.node, "job": ctx.job, "lock_hash": ctx.lock_hash, "lock_hash8": ctx.lock_hash8,
        APPLY_STATUS_KEY: APPLY_STARTED, "scope": scope_json(ctx.scope), "warnings": [],
    })
    ctx.lock_path.write_text(json.dumps(ctx.lock, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    # 1. the lower layers IN THIS JOB'S SCOPE, in topological order, each built AND packed at
    #    its ws version (A4).
    for entry in build_plan(ctx, edges):
        node_id = entry["node"]
        clone_dir = ctx.clone_dir(node_id)
        if clone_dir.exists():
            shutil.rmtree(str(clone_dir), ignore_errors=True)
        clone_dir.mkdir(parents=True, exist_ok=True)
        git_env = dict(os.environ, **_GIT_ENV)
        require_tool("git", "the leg clones each lower node at the lock sha")
        for argv in entry["clone"]:
            if run(argv, env=git_env) != 0:
                raise Refusal("clone of %s at %s failed" % (node_id, entry["sha"]))
        write_nuget_override(ctx, clone_dir, entry["override"])
        npm_preinstall(ctx, clone_dir, entry["npm"])
        env = ctx.command_env()
        for phase in ("build", "pack"):
            commands = [entry["build"]] if phase == "build" else entry["pack"]
            for cmd in [c for c in commands if c]:
                tool = _tool_for(cmd)
                if tool:
                    require_tool(tool, "the %s step of %s needs it" % (phase, node_id))
                if run(cmd, cwd=clone_dir, env=env, shell=True) != 0:
                    raise Refusal("`%s` failed for %s (%s)" % (cmd, node_id, phase))
        if RECIPES[node_id]["artifacts"].get("binary", {}).get("kind") == "server-binary":
            _stamp_server_marker(ctx, node_id, entry)
        for expected in entry["expect"]:
            if not Path(expected).exists():
                raise Refusal(
                    "%s packed with exit 0 but %s is missing — the feed would silently serve "
                    "nothing for this artifact" % (node_id, expected)
                )
        log("chain: %s built and packed at %s" % (node_id, entry["ws_version"]))

    exported = {}
    twins = []
    # 1b. the twins the lock pins, each cloned at the LOCK's sha — never a branch. A bogus or
    #     unreachable sha fails the fetch, and that is a refusal (exit 2), not a fallback. A twin
    #     the lock RELEASES has no sha: nothing is cloned or exported, and `record` names the gap.
    for twin_entry in twin_plan(ctx):
        twin = twin_entry["twin"]
        twin_dir = ctx.twin_dir(twin["node"])
        if twin_dir.exists():
            shutil.rmtree(str(twin_dir), ignore_errors=True)
            if twin_dir.exists():
                raise Refusal(
                    "twin %s: %s survived its delete (a locked or read-only file); refusing to clone "
                    "into a stale checkout" % (twin_repo(twin), twin_entry["dir"])
                )
        twin_dir.parent.mkdir(parents=True, exist_ok=True)
        require_tool("git", "the leg clones the %s twin at the lock sha" % twin_repo(twin))
        git_env = dict(os.environ, **_GIT_ENV)
        for argv in twin_entry["clone"]:
            status = run(argv, env=git_env)
            if status != 0:
                raise Refusal(
                    "twin %s: `%s` exited %s: clone of %s at lock.nodes.%s.sha %s failed (never "
                    "falling back to a branch)"
                    % (twin_repo(twin), " ".join(argv), status, twin_slug(twin), twin["node"], twin_entry["sha"])
                )
        exported[twin_entry["env"]] = twin_entry["dir"]
        twins.append({"node": twin["node"], "sha": twin_entry["sha"], "dir": twin_entry["dir"], "env": twin_entry["env"]})
        log("chain: twin %s @ %s -> %s (%s)" % (twin_repo(twin), twin_entry["sha"], twin_entry["dir"], twin_entry["env"]))
    for row in twin_skips(ctx):
        log("chain: skip %s: %s" % (row["step"], row["reason"]))

    # 2. this node's own override, at the CHECKOUT ROOT.
    written = []
    notes = []
    modes = sorted({edge["mode"] for edge in edges})
    nuget_edges = [e for e in edges if e["mode"] == "nuget"]
    if nuget_edges:
        rows = nuget_rows_for(ctx, nuget_edges)
        written.extend(write_nuget_override(ctx, ctx.checkout, rows))
        notes.extend("nuget: %s -> [%s]" % (r["package_id"], r["ws_version"]) for r in rows)
    unity_edges = [e for e in edges if e["mode"] == "unity-dll"]
    if unity_edges:
        written.extend(apply_unity_dll(ctx, unity_edges))
    pnpm_edges = [e for e in edges if e["mode"] == "pnpm"]
    if pnpm_edges:
        files, pnpm_notes = apply_pnpm(ctx, pnpm_edges)
        written.extend(files)
        notes.extend(pnpm_notes)
    server_edges = [e for e in edges if e["mode"] == "server-binary"]
    if server_edges:
        exported.update(server_env(ctx, server_edges))
    npm_edges = [e for e in edges if e["mode"] == "npm"]
    written.extend(npm_preinstall(ctx, ctx.checkout, npm_edges))
    for edge in npm_edges:
        notes.append("npm: %s pins the ws tarball; the workflow's `npm ci` installs it"
                     % (edge.get("path") or "."))

    # 3. the environment every later step in this job inherits (§C4).
    exported[NUGET_PROPERTY] = "true"
    exported[NUGET_PACKAGES_ENV] = ctx.nuget_packages_dir
    exported["CHAIN_ACTIVE"] = "1"
    exported["CHAIN_LOCK_PATH"] = as_posix_abs(ctx.lock_path)
    build_props = own_build_props(ctx)
    if build_props:
        exported["CHAIN_BUILD_PROPS"] = build_props
    ws_export = own_ws_version_export(ctx)
    if ws_export:
        exported["CHAIN_WS_VERSION"] = ws_export
    _write_github_env(sorted(exported.items()))
    _write_github_output([("active", "true")])

    state = {
        "node": ctx.node,
        "job": ctx.job,
        "lock_hash": ctx.lock_hash,
        "lock_hash8": ctx.lock_hash8,
        "modes": modes,
        "overrides": written,
        "notes": notes,
        "twins": twins,
        "env": exported,
        "feed_build_seconds": round(time.time() - started, 3),
        "scope": scope_json(ctx.scope),
        "skipped": _default_skips(ctx),
        "warnings": [],
        APPLY_STATUS_KEY: APPLY_OK,
    }
    _save_state(ctx, state)
    for line in notes:
        log("chain: " + line)
    log("chain: override active for %s (modes: %s)" % (ctx.node, ", ".join(modes) or "none in this job's scope"))
    return EXIT_OK


def _default_skips(ctx):
    """The honest, NAMED gaps a leg is allowed to have (A22, `04` §1).

    A `docker build` restores inside the container from nuget.org at the csproj pins, so an
    image built under an override would be GREEN against RELEASED bits. The leg records that
    as a skip rather than pretending the image was covered.
    """
    skips = []
    if ctx.node == "gamedev-mcp-server":
        skips.append({"step": "docker build", "reason": "image: n/a (not overridden) — A22"})
    if ctx.node == "cloud-ags":
        skips.append({"step": "nlog-drift", "reason": "nlog-drift: n/a (image not overridden)"})
    return skips


def _stamp_server_marker(ctx, node_id, entry):
    ws_version = ctx.ws_version(node_id)
    rid = host_rid()
    artifact_id = RECIPES[node_id]["artifacts"]["binary"]["id"]
    exe = server_exe_path(ctx, artifact_id, ws_version, rid)
    if not exe.is_file():
        raise Refusal(
            "`dotnet publish` exited 0 for %s/%s but %s is missing" % (node_id, rid, as_posix_abs(exe))
        )
    publish_cmd = next((c for c in entry["pack"] if "publish" in c), "")
    write_server_marker(
        exe.parent,
        ws_version=ws_version,
        sha=ctx.sha(node_id) or "",
        rid=rid,
        publish_cmd=publish_cmd,
        exe=exe,
    )


def _installed_npm_version(target, package_id):
    """`node_modules/<id>/package.json` `version` under `target`, or None if absent/unreadable."""
    manifest = target.joinpath("node_modules", *package_id.split("/")) / "package.json"
    try:
        data = json.loads(read_text(manifest).lstrip("﻿"))
    except (OSError, ValueError):
        return None
    return data.get("version") if isinstance(data, dict) else None


@command("apply-npm", "install the ws cli-core tarball into one CLI directory (AFTER its npm ci)")
def cmd_apply_npm(args):
    ctx = build_context(args)
    if ctx is None:
        return EXIT_OK
    target_rel = getattr(args, "dir", None) or "."
    target = ctx.checkout / target_rel
    edges = [e for e in overridden_edges(ctx) if e["mode"] == "npm"]
    if not edges:
        log("chain: %s has no npm edge inside this lock" % ctx.node)
        return EXIT_OK
    require_tool("npm", "the engine CLI leg installs the ws cli-core tarball")
    installed_any = False
    for edge, plan in zip(edges, npm_preinstall_plans(ctx, ctx.checkout, edges)):
        package_id, tarball = plan["package_id"], plan["tarball"]
        ws_version = ctx.ws_version(edge["node"])
        if not tarball.is_file():
            raise Refusal(
                "tarball missing for %s -> %s (never falling back to the registry)"
                % (edge["ref"], as_posix_abs(tarball))
            )
        if _installed_npm_version(target, package_id) == ws_version:
            # v4: `apply` already pinned the tarball, so the workflow's `npm ci` installed it
            log("chain: npm %s already holds %s@%s (pinned by apply)"
                % (as_posix_abs(target), package_id, ws_version))
            continue
        installed_any = True
        # an argv list, not a `shell=True` string: the tarball path carries the lock's ws version
        # (validated at load). On Windows `npm.cmd` still runs through cmd.exe, so the validation is
        # what keeps that value inert there, not the argv form.
        argv = ["npm", "install", as_posix_abs(tarball), "--no-save"]
        cmd = " ".join(argv)
        if run(argv, cwd=target, env=ctx.command_env()) != 0:
            raise Refusal("`%s` failed in %s" % (cmd, as_posix_abs(target)))
        log("chain: npm --no-save %s -> %s" % (as_posix_abs(target), as_posix_abs(tarball)))
    state = _load_state(ctx)
    state.setdefault("overrides", []).append(
        ("npm --no-save @ %s" if installed_any else "npm pinned by apply @ %s") % as_posix_abs(target))
    state.setdefault("npm_dirs", []).append(target_rel)
    _save_state(ctx, state)
    return EXIT_OK


@command("dry-run", "print the exact clone/build/pack/override plan — zero side effects")
def cmd_dry_run(args):
    ctx = build_context(args)
    if ctx is None:
        return EXIT_OK
    log("chain: DRY RUN — nothing is cloned, built, packed or written")
    log("chain: node        %s" % ctx.node)
    log("chain: lock        %s (%s)" % (ctx.lock_hash8, ctx.lock_hash or "no lock_hash"))
    log("chain: checkout    %s" % as_posix_abs(ctx.checkout))
    log("chain: feeds       %s" % as_posix_abs(ctx.artifacts))
    log("chain: %s %s" % (NUGET_PACKAGES_ENV, ctx.nuget_packages_dir))
    if ctx.scope is not None:
        log("chain: --edges     %s" % scope_label(ctx.scope))
    if not leg_is_active(ctx):
        log("chain: INACTIVE — %s consumes nothing inside this lock and is not a root node at a ws "
            "version; apply would write nothing and report active=false" % ctx.node)
        return EXIT_OK
    edges, outside = scoped_edges(ctx)
    own, own_outside = own_pack_in_scope(ctx)
    if is_root_node(ctx.node):
        log("")
        log("chain: --- %s @ %s (%s) — this node's OWN pack, run by the workflow after its tests ---"
            % (ctx.node, ctx.ws_version(ctx.node), ctx.sha(ctx.node)))
        values = placeholder_values(ctx, ctx.node, ctx.checkout)
        for raw in RECIPES[ctx.node]["pack"]:
            log("chain:   pack    %s" % substitute(raw, values, quote=shell_value))
        for _, body in own_packable_artifacts(ctx.node):
            # checkout-relative: the same `.artifacts/<feed>/<file>` the workflow's pack step writes
            relative = feed_artifact_path(ctx, ctx.node, body).relative_to(ctx.checkout).as_posix()
            log("chain: root node: no lower layers; pack → %s" % relative)
    for _, body in own:
        log("chain:   prove   %s (own pack)" % as_posix_abs(feed_artifact_path(ctx, ctx.node, body)))
    for row in scope_skips(ctx, edges, outside, own_outside):
        log("chain:   skip    %s: %s" % (row["step"], row["reason"]))
    twins = twin_plan(ctx)
    for entry in twins:
        twin, sha = entry["twin"], entry["sha"]
        log("")
        log("chain: twin %s @ %s — job %s of %s" % (twin_repo(twin), sha, twin["job"], twin["workflow"]))
        for argv in entry["clone"]:
            log("chain:   clone   %s" % " ".join(str(a) for a in argv))
        log("chain:   export  %s=%s" % (entry["env"], entry["dir"]))
        log("chain:   prove   %s: git -C %s rev-parse HEAD == %s" % (twin_consumer(twin), entry["dir"], sha))
    for row in twin_skips(ctx):
        log("chain:   skip    %s: %s" % (row["step"], row["reason"]))
    for entry in build_plan(ctx, edges):
        log("")
        log("chain: --- %s @ %s (%s) ---" % (entry["node"], entry["ws_version"], entry["sha"]))
        for argv in entry["clone"]:
            log("chain:   clone   %s" % " ".join(str(a) for a in argv))
        for row in entry["override"]:
            log("chain:   nuget   %s -> [%s]" % (row["package_id"], row["ws_version"]))
        for plan in npm_preinstall_plans(ctx, Path(entry["dir"]), entry["npm"]):
            log("chain:   npm     " + describe_npm_preinstall(plan))
        if entry["build"]:
            log("chain:   build   %s" % entry["build"])
        for cmd in entry["pack"]:
            log("chain:   pack    %s" % cmd)
        for expected in entry["expect"]:
            log("chain:   expect  %s" % expected)
    log("")
    log("chain: --- override for %s at the checkout root ---" % ctx.node)
    for mode in sorted({e["mode"] for e in edges}):
        mode_edges = [e for e in edges if e["mode"] == mode]
        if mode == "nuget":
            for row in nuget_rows_for(ctx, mode_edges):
                log("chain:   nuget   %s -> [%s]" % (row["package_id"], row["ws_version"]))
            log("chain:   write   %s" % as_posix_abs(ctx.checkout / NUGET_CONFIG_NAME))
            log("chain:   write   %s" % as_posix_abs(ctx.checkout / TARGETS_NAME))
        elif mode == "unity-dll":
            drops = []
            for edge in mode_edges:
                for drop in edge.get("drops") or ():
                    if drop not in drops:
                        drops.append(drop)
            for edge in mode_edges:
                package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
                log("chain:   unity   %s (%s)" % (unity_dll_name(package_id), ctx.ws_version(edge["node"])))
            for drop in drops:
                log("chain:   drop    %s (metas and %s untouched)" % (drop, UNITY_MANIFEST_NAME))
        elif mode == "pnpm":
            for plan in pnpm_plans(ctx, mode_edges):
                log("chain:   pnpm    %s -> %s" % (plan["package_id"], plan["spec"]))
            log("chain:   write   %s" % as_posix_abs(ctx.checkout / "pnpm-workspace.yaml"))
            log("chain:   run     %s" % PNPM_INSTALL_CMD)
        elif mode == "npm":
            for plan in npm_preinstall_plans(ctx, ctx.checkout, mode_edges):
                log("chain:   npm     " + describe_npm_preinstall(plan))
        elif mode == "server-binary":
            ws_version = ctx.ws_version("gamedev-mcp-server")
            artifact_id = RECIPES["gamedev-mcp-server"]["artifacts"]["binary"]["id"]
            exe = server_exe_path(ctx, artifact_id, ws_version, host_rid())
            for edge in mode_edges:
                log("chain:   server  %s=%s" % (edge.get("env") or "CHAIN_SERVER_PATH", as_posix_abs(exe)))
    log("")
    log("chain: --- $GITHUB_ENV ---")
    keys = ["CHAIN_ACTIVE=1", "CHAIN_LOCK_PATH=%s" % as_posix_abs(ctx.lock_path),
            "%s=true" % NUGET_PROPERTY, "%s=%s" % (NUGET_PACKAGES_ENV, ctx.nuget_packages_dir)]
    build_props = own_build_props(ctx)
    if build_props:
        keys.append("CHAIN_BUILD_PROPS=%s" % build_props)
    ws_export = own_ws_version_export(ctx)
    if ws_export:
        keys.append("CHAIN_WS_VERSION=%s" % ws_export)
    for entry in twins:
        keys.append("%s=%s" % (entry["env"], entry["dir"]))
    for key in keys:
        log("chain:   %s" % key)
    return EXIT_OK


@command("record", "write the per-job leg record and prove identity (exit 3 on any failure)")
def cmd_record(args):
    ctx = build_context(args)
    if ctx is None:
        return EXIT_OK
    state = _load_state(ctx)
    edges, outside = scoped_edges(ctx)
    own, own_outside = own_pack_in_scope(ctx)
    proofs = apply_status_proofs(ctx, state) + identity_proofs(ctx, edges, own) + _twin_proofs(ctx)
    skipped = list(state.get("skipped") or [])
    # The named gaps belong in the record whether or not `apply` wrote the state file: a leg
    # that reports NO skips is claiming coverage it does not have (A22). An edge this job's
    # `--edges` scope left out is one of them — never proven, never silently absent.
    for row in _default_skips(ctx) + scope_skips(ctx, edges, outside, own_outside) + twin_skips(ctx):
        if row not in skipped:
            skipped.append(row)
    for raw in getattr(args, "skip", None) or ():
        step, _, reason = str(raw).partition("=")
        skipped.append({"step": step, "reason": reason or "unspecified"})
    warnings = list(state.get("warnings") or [])
    # `--warn TEXT` (repeatable): a named caveat the job wants in the record (a pin drift, a
    # step it could not reach). Every non-blank value lands, in order, repeats included. A
    # warning NEVER changes `result`.
    for raw in getattr(args, "warn", None) or ():
        text = str(raw).strip()
        if text:
            warnings.append(text)
    if "scope" in state and state.get("scope") != scope_json(ctx.scope):
        applied = state.get("scope")
        warnings.append(
            "apply ran with --edges %s but record with --edges %s: pass the SAME scope to both"
            % (scope_label(None if applied is None else frozenset(applied)), scope_label(ctx.scope))
        )
    failed = [p for p in proofs if not p["ok"]]
    job_status = (getattr(args, "job_status", None) or "").lower()
    record = {
        "schema": 1,
        "node": ctx.node,
        "lock_hash": ctx.lock_hash,
        "lock_hash8": ctx.lock_hash8,
        "repo": os.environ.get("GITHUB_REPOSITORY") or RECIPES[ctx.node]["slug"],
        "workflow": os.environ.get("GITHUB_WORKFLOW") or "",
        "job": ctx.job or os.environ.get("GITHUB_JOB") or "",
        "matrix": getattr(args, "matrix", None) or "",
        "os": runner_os(),
        "engine": getattr(args, "engine", None) or "",
        "engine_version": getattr(args, "engine_version", None) or "",
        "tier": getattr(args, "tier", None) or "T0",
        "run_id": os.environ.get("GITHUB_RUN_ID") or "",
        "run_attempt": os.environ.get("GITHUB_RUN_ATTEMPT") or "",
        "head_sha": observed_head_sha(),
        "event": os.environ.get("GITHUB_EVENT_NAME") or "",
        "feed_script_sha256": feed_script_sha256(),
        "feed_build_seconds": state.get("feed_build_seconds", 0),
        "overrides": list(state.get("overrides") or []),
        "identity": proofs,
        "skipped": skipped,
        "warnings": warnings,
        # GitHub's job.status is success | failure | cancelled: a CANCELLED job's tests never
        # finished, so only a successful job (or none given) may record green.
        "result": "red" if (failed or job_status not in ("", "success")) else "green",
    }
    out = Path(getattr(args, "out", None) or "chain-leg.json")
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(record, indent=2, sort_keys=False) + "\n", encoding="utf-8")
    _write_github_output([("artifact_name", artifact_name(ctx, record)), ("result", record["result"])])
    for proof in proofs:
        log("chain: %s %s <- %s: expected %r, observed %r (%s)" % (
            "OK  " if proof["ok"] else "FAIL", proof["consumer"], proof["source"],
            proof["expected"], proof["resolved"], proof["package"],
        ))
    log("chain: leg record -> %s (%d proof(s), %d failing)" % (as_posix_abs(out), len(proofs), len(failed)))
    if failed:
        log("chain: identity FAILED — this leg is RED even though its tests may have passed")
        return EXIT_IDENTITY
    return EXIT_OK


def runner_os():
    value = (os.environ.get("RUNNER_OS") or platform.system() or "").lower()
    return {"linux": "linux", "windows": "windows", "macos": "macos", "darwin": "macos"}.get(value, value)


def artifact_name(ctx, record):
    """`chain-leg-<node>-<os>-<job>[-<matrix suffix>]` — one name per JOB.

    v4 artifacts are immutable and a second matrix job uploading the same name FAILS (B4), so
    MPD x3, Unity x12 and Godot x24 each need a distinct one.
    """
    def clean(value):
        # The name reaches `$GITHUB_OUTPUT` and an artifact name: only `[A-Za-z0-9._-]` survives
        # (a GitHub job id already is exactly that, so a real name is unchanged).
        return re.sub(r"[^A-Za-z0-9._-]+", "-", str(value)).strip("-")

    parts = ["chain-leg", record["node"], clean(record["os"] or "unknown"), clean(record["job"] or "job")]
    suffix = record.get("matrix") or ""
    if suffix:
        parts.append(clean(suffix))
    return "-".join(p for p in parts if p)


def normalize_newlines(data):
    """`\\r\\n` / `\\r` -> `\\n`.

    Every checkout of this file is a git checkout, and this workspace runs
    `core.autocrlf=true`: the SAME vendored script is CRLF on a Windows runner and LF on a
    hosted ubuntu one. A raw byte digest would therefore report drift for nine repos on one
    platform and none on the other — a gate that is red by construction. Same rationale as
    `manifest.manifest_sha256` and `overrides/unity_dll._normalize_newlines`.
    """
    return data.replace(b"\r\n", b"\n").replace(b"\r", b"\n")


def feed_script_sha256(path=None):
    """The identity of the running leg script, line-ending independent."""
    try:
        return sha256_bytes(normalize_newlines(Path(path or __file__).resolve().read_bytes()))
    except OSError:  # pragma: no cover - the script is always readable
        return ""


# ----------------------------------------------------------------------
# §C9 identity proofs — every row names the ARTIFACT it was read from
# ----------------------------------------------------------------------


def _proof(consumer, source, package, resolved, expected, ok):
    return {
        "consumer": consumer,
        "source": source,
        "package": package,
        "resolved": resolved,
        "expected": expected,
        "ok": bool(ok),
    }


def _assets_candidates(project):
    """Where a `project.assets.json` can live, most specific first.

    `Godot.NET.Sdk` redirects `BaseIntermediateOutputPath` to `.godot/mono/temp/obj/` NEXT TO
    THE PROJECT, so a reader that only knows `obj/` reports nothing for the very projects the
    Godot legs build (A7) — and "nothing" reads exactly like a clean run.
    """
    directory = project.parent
    godot = directory / ".godot" / "mono" / "temp" / "obj"
    out = [
        (directory / "obj" / "project.assets.json", "obj/project.assets.json"),
        (godot / "project.assets.json", ".godot/mono/temp/obj/project.assets.json"),
        (godot / project.stem / "project.assets.json", ".godot/mono/temp/obj/<project>/project.assets.json"),
    ]
    if godot.is_dir():
        seen = {p for p, _ in out}
        for found in sorted(godot.rglob("project.assets.json")):
            if found not in seen:
                out.append((found, ".godot/mono/temp/obj/**/project.assets.json"))
    return out


def _library_versions(payload):
    out = {}
    for key in (payload.get("libraries") or {}):
        name, _, version = str(key).partition("/")
        if name and version:
            out[name] = version
    return out


def _nuget_proofs(ctx, edges):
    proofs = []
    rows = nuget_rows_for(ctx, edges)
    expected_by_id = {row["package_id"]: row["ws_version"] for row in rows}
    for edge in edges:
        package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
        expected = expected_by_id.get(package_id)
        for pin in pins_in_scope(edge, ctx.scope):
            project = ctx.checkout / pin
            found = None
            for path, label in _assets_candidates(project):
                if path.is_file():
                    found = (path, label)
                    break
            if found is None:
                # An IN-SCOPE consumer that restored nothing is not evidence of anything: a job
                # whose restore never ran would otherwise record a vacuous GREEN. A job that
                # genuinely does not restore this project (`Godot-Tests.csproj` is not in the
                # sln; only the engine legs build it) names the pins it DOES restore in --edges.
                proofs.append(_proof(
                    pin,
                    "no project.assets.json for %s in this checkout: nothing restored for an "
                    "in-scope consumer (if this job does not restore it, select only the "
                    "consumers it does with --edges <pin>)" % pin,
                    package_id, None, expected, False,
                ))
                continue
            path, label = found
            try:
                versions = _library_versions(json.loads(path.read_text(encoding="utf-8-sig")))
            except (OSError, ValueError) as exc:
                proofs.append(_proof(pin, "%s unreadable: %s" % (label, exc), package_id, None, expected, False))
                continue
            observed = versions.get(package_id)
            proofs.append(_proof(pin, label, package_id, observed, expected, observed == expected))
    return proofs


def _unity_proofs(ctx, edges):
    """sha256(dropped DLL) == sha256 of the same DLL inside the ws nupkg (byte identity)."""
    proofs = []
    for edge in edges:
        package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
        ws_version = ctx.ws_version(edge["node"])
        dll_name = unity_dll_name(package_id)
        nupkg = find_nupkg(ctx.feed_nuget, package_id, ws_version)
        if nupkg is None:
            proofs.append(_proof(
                ctx.node, "no ws nupkg in %s" % as_posix_abs(ctx.feed_nuget),
                package_id, None, ws_version, False,
            ))
            continue
        expected_sha = sha256_bytes(extract_lib_dll(nupkg, dll_name))
        for drop in edge.get("drops") or ():
            target = ctx.checkout / drop / dll_name
            if not target.is_file():
                proofs.append(_proof(
                    "%s/%s" % (drop, dll_name), "missing on disk", package_id, None, expected_sha, False
                ))
                continue
            observed = sha256_file(target)
            proofs.append(_proof(
                "%s/%s" % (drop, dll_name),
                "sha256 vs %s!%s" % (nupkg.name, UNITY_LIB_ENTRY.format(name=dll_name)),
                package_id, observed, expected_sha, observed == expected_sha,
            ))
        # The manifest MUST stay at the pin (A2): a ws value there makes the in-editor
        # resolver re-download the released nupkg and delete these very DLLs.
        for drop in edge.get("drops") or ():
            manifest_path = ctx.checkout / drop / UNITY_MANIFEST_NAME
            if not manifest_path.is_file():
                continue
            try:
                data = json.loads(manifest_path.read_text(encoding="utf-8"))
            except (OSError, ValueError):
                continue
            recorded = ((data.get("packages") or {}).get(package_id) or {}).get("version")
            pin = (ws_version or "").split("-ws.g", 1)[0]
            proofs.append(_proof(
                "%s/%s" % (drop, UNITY_MANIFEST_NAME),
                "%s[%s].version stays at the PIN (A2)" % (UNITY_MANIFEST_NAME, package_id),
                package_id, recorded, pin, recorded == pin,
            ))
    return proofs


def _npm_proofs(ctx, edges, pnpm=False):
    """The installed package's own `package.json` `version`, read BY PATH.

    Never `require.resolve('<pkg>/package.json')`: cli-core's `exports` is `"."` only, so it
    throws `ERR_PACKAGE_PATH_NOT_EXPORTED` — a proof method that cannot run is not a proof.
    """
    proofs = []
    state = _load_state(ctx)
    for edge in edges:
        package_id = RECIPES[edge["node"]]["artifacts"][edge["artifact"]]["id"]
        expected = ctx.ws_version(edge["node"])
        bases = []
        if pnpm:
            for pin in edge.get("pins") or ():
                candidate = ctx.checkout / Path(str(pin).replace("\\", "/")).parent
                if candidate.is_dir() and candidate not in bases:
                    bases.append(candidate)
        else:
            bases.append(ctx.checkout / (edge.get("path") or "."))
            for extra in state.get("npm_dirs") or ():
                candidate = ctx.checkout / extra
                if candidate.is_dir() and candidate not in bases:
                    bases.append(candidate)
        if not bases:
            bases = [ctx.checkout]
        for base in bases:
            package_dir = base.joinpath("node_modules", *[p for p in package_id.split("/") if p])
            manifest_path = package_dir / "package.json"
            if not manifest_path.is_file():
                proofs.append(_proof(
                    as_posix_abs(base), "no node_modules/%s/package.json" % package_id,
                    package_id, None, expected, False,
                ))
                continue
            try:
                observed = json.loads(manifest_path.read_text(encoding="utf-8")).get("version")
            except (OSError, ValueError) as exc:
                proofs.append(_proof(
                    as_posix_abs(base), "package.json unreadable: %s" % exc,
                    package_id, None, expected, False,
                ))
                continue
            source = "%s/package.json version" % as_posix_abs(package_dir)
            if pnpm:
                # All four edges must land on ONE store entry: record the realpath so a
                # second entry is visible in the record rather than inferred.
                source += " (realpath %s)" % as_posix_abs(package_dir)
            proofs.append(_proof(
                as_posix_abs(base), source, package_id, observed, expected, observed == expected
            ))
    return proofs


def _server_proofs(ctx, edges):
    ws_version = ctx.ws_version("gamedev-mcp-server")
    rid = host_rid()
    artifact_id = RECIPES["gamedev-mcp-server"]["artifacts"]["binary"]["id"]
    exe = server_exe_path(ctx, artifact_id, ws_version, rid)
    marker = exe.parent / SERVER_MARKER_NAME
    expected_sha = ctx.sha("gamedev-mcp-server") or ""
    proofs = []
    if not marker.is_file():
        return [_proof(ctx.node, "%s missing" % as_posix_abs(marker), artifact_id, None, ws_version, False)]
    try:
        data = json.loads(marker.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        return [_proof(ctx.node, "%s unreadable: %s" % (SERVER_MARKER_NAME, exc), artifact_id, None, ws_version, False)]
    missing_fields = [f for f in SERVER_MARKER_FIELDS if f not in data]
    proofs.append(_proof(
        ctx.node, "%s field set" % SERVER_MARKER_NAME, artifact_id,
        ",".join(sorted(data)), ",".join(SERVER_MARKER_FIELDS), not missing_fields,
    ))
    proofs.append(_proof(
        ctx.node, "%s.ws_version" % SERVER_MARKER_NAME, artifact_id,
        data.get("ws_version"), ws_version, data.get("ws_version") == ws_version,
    ))
    proofs.append(_proof(
        ctx.node, "%s.sha" % SERVER_MARKER_NAME, artifact_id,
        data.get("sha"), expected_sha, data.get("sha") == expected_sha,
    ))
    if exe.is_file():
        observed = sha256_file(exe)
        proofs.append(_proof(
            ctx.node, "%s.exe_sha256 vs the exe on disk" % SERVER_MARKER_NAME, artifact_id,
            observed, data.get("exe_sha256"), observed == data.get("exe_sha256"),
        ))
    else:
        proofs.append(_proof(
            ctx.node, "exe missing at %s" % as_posix_abs(exe), artifact_id, None, data.get("exe_sha256"), False
        ))
    exe_str = as_posix_abs(exe)
    for edge in edges:
        var = edge.get("env") or "CHAIN_SERVER_PATH"
        observed = (os.environ.get(var) or "").replace("\\", "/")
        proofs.append(_proof(
            ctx.node, "%s points at the packed exe" % var, artifact_id, observed or None, exe_str,
            observed == exe_str,
        ))
    return proofs


def _own_dll_name(package_id):
    """`com.IvanMurzak.McpPlugin.Common` -> `McpPlugin.Common.dll`: the assembly a nupkg ships for itself."""
    return (package_id[len(UNITY_PACKAGE_PREFIX):] if package_id.startswith(UNITY_PACKAGE_PREFIX) else package_id) + ".dll"


def _own_nupkg_dll(archive, package_id):
    """The package's own assembly inside its nupkg: `<Name>.dll` under `lib/netstandard2.1/`
    first, then under any other `lib/<tfm>/`. `None` when no `lib/` entry carries that name —
    another assembly's `ProductVersion` is never evidence for this package."""
    wanted = _own_dll_name(package_id)
    by_name = sorted(
        n for n in archive.namelist()
        if n.startswith("lib/") and n.rsplit("/", 1)[-1].lower() == wanted.lower()
    )
    preferred = [n for n in by_name if n.lower() == UNITY_LIB_ENTRY.format(name=wanted).lower()]
    for group in (preferred, by_name):
        if group:
            return group[0]
    return None


def _own_pack_proofs(ctx, artifacts):
    """A ROOT leg's evidence: its OWN pack at the lock's ws version (§C4 root nodes).

    Read from the artifact, never an exit code: a nupkg's NAME plus the `ProductVersion` its
    DLL was actually built at (`pack --no-build -p:Version=X` after a build at Y names the file X
    and stamps Y — A4); a tarball's own `package/package.json` `version`.
    """
    proofs = []
    ws_version = ctx.ws_version(ctx.node)
    for _, body in artifacts:
        package_id = body["id"]
        path = feed_artifact_path(ctx, ctx.node, body)
        found = None
        if path.parent.is_dir():
            for candidate in sorted(path.parent.iterdir()):
                if candidate.name.lower() == path.name.lower() and candidate.is_file():
                    found = candidate
                    break
        if found is None:
            proofs.append(_proof(
                ctx.node, "no %s in %s (this leg's own pack produced nothing)"
                % (path.name, as_posix_abs(path.parent)), package_id, None, ws_version, False,
            ))
            continue
        if body["kind"] == "tgz":
            try:
                observed = json.loads(read_tarball_member(found, "package.json").decode("utf-8")).get("version")
            except Exception as exc:  # a corrupt tarball is a failing proof, never a crash
                proofs.append(_proof(ctx.node, "%s unreadable: %s" % (found.name, exc), package_id, None, ws_version, False))
                continue
            proofs.append(_proof(
                ctx.node, "%s!package/package.json version" % found.name, package_id,
                observed, ws_version, observed == ws_version,
            ))
            continue
        try:
            with zipfile.ZipFile(str(found)) as archive:
                member = _own_nupkg_dll(archive, package_id)
                data = archive.read(member) if member else None
        except (OSError, zipfile.BadZipFile, KeyError) as exc:
            proofs.append(_proof(ctx.node, "%s unreadable: %s" % (found.name, exc), package_id, None, ws_version, False))
            continue
        if data is None:
            proofs.append(_proof(
                ctx.node, "%s has no lib/<tfm>/%s" % (found.name, _own_dll_name(package_id)),
                package_id, None, ws_version, False,
            ))
            continue
        try:
            product = pe_version_strings_from_bytes(data).get("ProductVersion")
        except (PeFormatError, struct.error, IndexError, ValueError) as exc:
            proofs.append(_proof(
                ctx.node, "%s!%s ProductVersion unreadable: %s" % (found.name, member, exc),
                package_id, None, ws_version, False,
            ))
            continue
        # SourceLink appends `+<sha>`; the version half is what `-p:Version` stamped.
        core = (product or "").split("+", 1)[0] or None
        proofs.append(_proof(
            ctx.node, "%s!%s ProductVersion %r" % (found.name, member, product),
            package_id, core, ws_version, core == ws_version,
        ))
    return proofs


def identity_proofs(ctx, edges, own):
    """§C9 — one row per consumer/source, never an exit code.

    `edges` / `own` are this job's in-scope halves of `scoped_edges` / `own_pack_in_scope`.
    """
    proofs = []
    by_mode = {}
    for edge in edges:
        by_mode.setdefault(edge["mode"], []).append(edge)
    for mode in sorted(by_mode):
        group = by_mode[mode]
        if mode == "nuget":
            proofs.extend(_nuget_proofs(ctx, group))
        elif mode == "unity-dll":
            proofs.extend(_unity_proofs(ctx, group))
        elif mode == "npm":
            proofs.extend(_npm_proofs(ctx, group, pnpm=False))
        elif mode == "pnpm":
            proofs.extend(_npm_proofs(ctx, group, pnpm=True))
        elif mode == "server-binary":
            proofs.extend(_server_proofs(ctx, group))
    if edges and not proofs:
        proofs.append(_proof(
            ctx.node, "NO identity evidence for this leg", "", None, None, False
        ))
    if own:
        proofs.extend(_own_pack_proofs(ctx, own))
    return proofs


# ----------------------------------------------------------------------
# §C1-C3 — the workflow fragment the nine dispatch PRs apply
# ----------------------------------------------------------------------


def to_format_expression(raw):
    """Turn an interpolated workflow string into ONE `format(...)` call.

    GitHub does not allow a nested `${{ }}`, so an existing
    `test-pr-${{ github.workflow }}-${{ github.ref }}` group cannot simply be pasted inside the
    combined conditional; it has to become
    `format('test-pr-{0}-{1}', github.workflow, github.ref)`.
    """
    text = str(raw)
    args = []
    template_parts = []
    last = 0
    for match in _EXPR_RE.finditer(text):
        template_parts.append(text[last:match.start()])
        template_parts.append("{%d}" % len(args))
        args.append(match.group(1).strip())
        last = match.end()
    template_parts.append(text[last:])
    template = "".join(template_parts)
    if not args:
        return "'%s'" % template.replace("'", "''")
    return "format('%s', %s)" % (template.replace("'", "''"), ", ".join(args))


def render_fragment(node, existing_group=None, existing_cancel=None, new_concurrency=None):
    """The §C1–C3 (+ §C4/§C5) YAML every `p2-dispatch-*` PR applies, node id substituted."""
    if node not in RECIPES:
        raise Refusal("unknown node %r" % node)
    if new_concurrency is None:
        new_concurrency = node in NEEDS_NEW_CONCURRENCY
    job = "<job-id>"
    selectors = scope_selectors(node)
    lines = [
        "# chain leg contract (p2-chain-feed-script SS C1-C5) for node: %s" % node,
        "# Vendored script: .github/scripts/chain_feed.py (CHAIN_FEED_VERSION %s)" % CHAIN_FEED_VERSION,
        "",
        "on:",
        "  workflow_dispatch:",
        "    inputs:",
        "      lock:",
        "        description: the chain lock JSON (<= %d characters)" % MAX_LOCK_CHARS,
        "        required: true",
        "        type: string",
        "      lock_hash8:",
        "        description: first 8 hex characters of the lock hash",
        "        required: true",
        "        type: string",
        "",
        "run-name: ${{ github.event_name == 'workflow_dispatch' && format('chain {0} %s', inputs.lock_hash8) || '' }}" % node,
    ]
    if existing_group is not None or new_concurrency:
        lines.append("")
        lines.append("concurrency:")
        chain_group = "format('chain-{0}-{1}', inputs.lock_hash8, github.workflow)"
        if existing_group is not None:
            fallback = to_format_expression(existing_group)
            lines.append(
                "  group: ${{ github.event_name == 'workflow_dispatch' && %s || %s }}"
                % (chain_group, fallback)
            )
            cancel = "true" if existing_cancel is None else str(existing_cancel).strip()
            lines.append(
                "  cancel-in-progress: ${{ github.event_name != 'workflow_dispatch' && %s }}" % cancel
            )
        else:
            lines.append(
                "  group: ${{ github.event_name == 'workflow_dispatch' && %s || format('%s-{0}', github.ref) }}"
                % (chain_group, node)
            )
            lines.append("  cancel-in-progress: false")
    lines += [
        "",
        "env:",
        "  CHAIN_LOCK: ${{ inputs.lock }}",
        "  CHAIN_LOCK_HASH8: ${{ inputs.lock_hash8 }}",
        "",
        "# --- in every job, AFTER checkout + toolchain setup and BEFORE the first restore/install.",
        "#     The interpreter is resolved ONCE per job and invoked through the ${{ env.CHAIN_PY }}",
        "#     EXPRESSION, which GitHub substitutes before any shell runs — so the same step text works",
        "#     under bash (hosted) and pwsh (Windows self-hosted, where `shell: bash` is WSL). Never a",
        "#     bare `python` — and never a bare interpreter NAME at all. Each resolver RUNS a candidate,",
        "#     which must import C-extension modules and print its own sys.executable, and exports THAT",
        "#     absolute, space-free, forward-slashed path (the output SHAPE is the verdict). A name is",
        "#     re-resolved on every call, and on the shared Windows runners `python`/`py` can resolve",
        "#     into ANOTHER runner's (possibly deleted) workspace, so `py` is never a candidate. The",
        "#     run-it-and-export-sys.executable idea follows ai-game-dev-software's",
        "#     .github/scripts/resolve-python.ps1.",
        "#     A runner with no usable Python on PATH (the Unreal self-hosted legs run UE's bundled",
        "#     python.exe) sets job-level `env: CHAIN_PY:` to an ABSOLUTE, SPACE-FREE interpreter path",
        "#     instead — the value is substituted unquoted — and both resolver steps below then skip.",
        "#     Every step id below is chain-prefixed and used ONCE per job: never reuse a job's own step id",
        "#     (e.g. a second `server` step to swap a downloaded binary for the chain one) — branch inside",
        "#     ONE step on env.CHAIN_ACTIVE instead.",
        "#     Both resolvers run under `always()`: the leg record below is `if: always()`, and a",
        "#     resolver skipped after an earlier failure would leave CHAIN_PY empty, so the RED record",
        "#     that failure deserves could never be written or uploaded.",
        "      - name: chain python (posix)",
        "        if: always() && runner.os != 'Windows' && env.CHAIN_PY == ''",
        "        shell: bash",
        "        run: |",
        "          for c in python3 python; do",
        "            exe=\"$(\"$c\" -c '%s' 2>/dev/null)\" || continue" % CHAIN_PY_PROBE,
        "            case \"$exe\" in /*) ;; *) continue ;; esac",
        "            case \"$exe\" in *[[:space:]]*) continue ;; esac",
        "            [ -f \"$exe\" ] && [ -x \"$exe\" ] || continue",
        "            echo \"CHAIN_PY=$exe\" >> \"$GITHUB_ENV\"; exit 0",
        "          done",
        "          echo 'chain: no Python 3.9+ on PATH that runs and reports an absolute, space-free sys.executable (tried python3, python)' >&2; exit 2",
        "      - name: chain python (windows)",
        "        if: always() && runner.os == 'Windows' && env.CHAIN_PY == ''",
        "        shell: pwsh",
        "        run: |",
        "          foreach ($c in 'python', 'python3') {",
        "            $cmd = Get-Command $c -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1",
        "            if (-not $cmd) { continue }",
        "            $global:LASTEXITCODE = $null",
        # `try`: a GitHub pwsh step runs under `$ErrorActionPreference = 'stop'`, so a candidate that
        # cannot even START (a 0-byte or half-deleted python.exe) would throw and end the step
        # instead of moving on to the next one.
        "            try { $out = & $cmd.Source -c '%s' 2>$null } catch { continue }" % CHAIN_PY_PROBE,
        "            if ($LASTEXITCODE -ne 0) { continue }",
        "            $exe = (@($out) -join ' ').Trim()",
        "            if ($exe -notmatch '^[A-Za-z]:/\\S+\\z' -or -not (Test-Path -LiteralPath $exe -PathType Leaf)) { continue }",
        "            \"CHAIN_PY=$exe\" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8; exit 0",
        "          }",
        "          [Console]::Error.WriteLine('chain: no Python 3.9+ on PATH that runs and reports an absolute, space-free sys.executable (tried python, python3; never py)'); exit 2",
        "      - name: chain feed",
        "        id: chain",
        "        run: ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py apply --node %s --job %s" % (node, job),
        "",
        "# --- per-job scope: a job that consumes only PART of this node's edges passes the SAME",
        "#     --edges selectors to `apply` AND `record` (repeatable, or comma-separated). Out-of-scope",
        "#     edges are not built, not overridden and not proven; `record` lists each in skipped[].",
        "#     Omit --edges in a job that consumes every edge. `none` = sha assertion + record only.",
        "#     A consumer csproj path selects just that project: a job that restores only SOME of an",
        "#     edge's projects names the ones it restores (an in-scope project with no",
        "#     project.assets.json is RED, never a pass).",
        "#     selectors for %s: %s" % (node, ", ".join(selectors)),
        "#     e.g.  ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py apply  --node %s --job %s --edges %s"
        % (node, job, selectors[0]),
        "#           ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py record --node %s --job %s --edges %s "
        "--job-status ${{ job.status }} --out chain-leg.json" % (node, job, selectors[0]),
    ]
    if SCOPE_SELF in selectors:
        lines.append(
            "#     root node: with no --edges `record` proves this node's OWN pack, so a job that does "
            "not pack passes --edges none"
        )
    for twin in node_twins(node):
        selector = twin_selector(twin)
        lines += [
            "#     twin: job `%s` of %s drives %s from source. It passes --edges %s (a twin is never "
            "implied by an absent --edges):" % (twin["job"], twin["workflow"], twin_repo(twin), selector),
            "#           ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py apply  --node %s --job %s --edges %s"
            % (node, twin["job"], selector),
            "#           ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py record --node %s --job %s --edges %s "
            "--job-status ${{ job.status }} --out chain-leg.json" % (node, twin["job"], selector),
            "#     `apply` clones %s at lock.nodes.%s.sha into $%s (a bogus sha fails `apply` at the fetch);"
            % (twin_repo(twin), twin["node"], twin_env(twin)),
            "#     the job's clone step uses that path when env.%s != '' (unset when the lock RELEASES the twin:"
            " a named skip), else its existing ref;" % twin_env(twin),
            "#     `record` proves identity[] {consumer: \"%s\", resolved: its rev-parse HEAD, expected: the lock sha}"
            % twin_consumer(twin),
        ]
    if any(edge["mode"] == "npm" for edge in RECIPES[node]["consumes"]):
        npm_dir = next(
            (edge.get("path") or "." for edge in RECIPES[node]["consumes"] if edge["mode"] == "npm"), "."
        )
        lines += [
            "",
            "# --- AFTER that directory's own `npm ci` (apply pinned the ws tarball before it; this re-checks):",
            "      - name: chain feed (npm)",
            "        if: env.CHAIN_ACTIVE == '1'",
            "        run: ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py apply-npm --node %s --dir %s" % (node, npm_dir),
        ]
    # The upload is keyed on `artifact_name` (written by every record that ran) rather than on
    # CHAIN_ACTIVE, which only a COMPLETED apply exports: otherwise a failed apply's red record would
    # never reach the verdict. A refusal before `record` writes anything (a malformed lock, the §C6
    # sha assertion) still uploads nothing — that leg is missing, not green.
    lines += [
        "",
        "# --- after the test step. `--job-status ${{ job.status }}` is what makes a leg whose tests failed",
        "#     or were cancelled record `result: red`; without it a record reflects only identity.",
        "#     `record --warn TEXT` (repeatable) adds a named caveat to warnings[]; it never turns a leg RED.",
        "#     The upload runs whenever the record was written, so a failed apply's red record uploads too.",
        "      - name: chain leg record",
        "        id: chain_record",
        "        if: always()",
        "        run: ${{ env.CHAIN_PY }} .github/scripts/chain_feed.py record --node %s --job %s "
        "--job-status ${{ job.status }} --out chain-leg.json" % (node, job),
        "      - name: chain leg artifact",
        "        if: always() && steps.chain_record.outputs.artifact_name != ''",
        "        uses: actions/upload-artifact@%s   # or the major this workflow already uses (>= v4, C5)" % UPLOAD_ARTIFACT_MAJOR,
        "        with:",
        "          name: ${{ steps.chain_record.outputs.artifact_name }}",
        "          path: chain-leg.json",
        "",
    ]
    return "\n".join(lines)


@command("print-fragment", "print the C1-C5 workflow fragment for this node")
def cmd_print_fragment(args):
    print(render_fragment(
        getattr(args, "node", None),
        existing_group=getattr(args, "existing_group", None),
        existing_cancel=getattr(args, "existing_cancel", None),
        new_concurrency=getattr(args, "new_concurrency", None) or None,
    ), end="")
    return EXIT_OK


# ----------------------------------------------------------------------
# fetch-fixtures — the T2 replay inputs (`p2-replay-leg`)
# ----------------------------------------------------------------------
#
# A T2 consumer (the App, the cloud host) replays each engine's COMMITTED fixture through the
# null-engine host (`--replay <fixture>`, MCP-Plugin-dotnet `docs/chain-fixtures.md` F6). The
# fixtures live in each ENGINE repo under `tests/chain-fixtures/<engine-version>/tools.jsonl`, so
# this fetches them AT THE LOCK'S SHA of each engine node — never a branch, never `main` — and lays
# them out as `<out>/<engine>/<engine_version>/<surface>/tools.jsonl`, keyed on each file's OWN
# `meta` line, plus `<out>/fixtures.json` (what a consumer iterates). MCP-Plugin-dotnet's reference
# sample is fetched too, as the self-check fixture.
#
# `.scripts/chain/replay.py` (the dev box) calls THESE functions with a `gh`-backed transport, so
# the operator's fetch and a leg's fetch are one piece of code, not two that can drift.

#: `(node id, meta.engine)` for every engine node whose committed fixtures a T2 consumer replays.
FIXTURE_ENGINES = (("unity-mcp", "unity"), ("godot-mcp", "godot"), ("unreal-mcp", "unreal"))
FIXTURE_REFERENCE_NODE = "mcp-plugin-dotnet"
FIXTURE_REFERENCE_ENGINE = "null-engine"
FIXTURE_ROOT = "tests/chain-fixtures"
FIXTURE_FILE = "tools.jsonl"
FIXTURE_SCHEMA = 1
FIXTURES_MANIFEST = "fixtures.json"
#: The one environment name a T2 consumer reads: the fetched directory holding `fixtures.json`.
REPLAY_ENV = "CHAIN_REPLAY_FIXTURES"
#: The literal skip name of a T2 test whose `CHAIN_REPLAY_FIXTURES` is absent.
REPLAY_SKIP_NAME = "chain-replay-fixtures-absent"
GITHUB_API_URL = "https://api.github.com"
#: A listed directory name, `meta.engine_version` and `meta.surface` each become a PATH segment of
#: the layout, and all three come from a remote repository: one shape, no separators, no `..`.
_FIXTURE_SEGMENT_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}\Z")
#: A UTF-8 BOM. A fixture may start with one on the wire (F1: `fixture_meta` tolerates it); it is
#: spelled as a named bytes constant — never an invisible character inside a string literal, which
#: is exactly the edit a well-meant "cleanup" silently breaks.
_UTF8_BOM = b"\xef\xbb\xbf"


def fixture_segment(value):
    """`value` when it is safe as ONE path segment of the fixture layout, else `None`."""
    if not isinstance(value, str) or ".." in value or not _FIXTURE_SEGMENT_RE.match(value):
        return None
    return value


class _NoCrossHostAuthRedirect(urllib.request.HTTPRedirectHandler):
    """Strip `Authorization` when a redirect crosses to a different host.

    urllib's stock redirect handler copies every request header except the content-* ones, so a
    redirect from `api.github.com` to any other host would replay the Bearer token there.
    Contents listings only redirect same-host today, so this is hardening against that changing,
    not a live leak: a same-host redirect keeps the header, a cross-host one loses it.
    """

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        new_request = super().redirect_request(req, fp, code, msg, headers, newurl)
        if new_request is not None and urllib.parse.urlsplit(new_request.full_url).netloc != urllib.parse.urlsplit(req.full_url).netloc:
            for name in [key for key in new_request.headers if key.lower() == "authorization"]:
                del new_request.headers[name]
        return new_request


#: The transport's default opener. `build_opener` prefers a passed handler that SUBCLASSES a stock
#: one, so this replaces urllib's own `HTTPRedirectHandler` instead of stacking beside it.
_OPENER = urllib.request.build_opener(_NoCrossHostAuthRedirect())


def _http_error_body(exc):
    """The error response's body, or `b""` when this `HTTPError` carries none (`fp=None`)."""
    if getattr(exc, "fp", None) is None:
        return b""
    try:
        return exc.read() or b""
    except (OSError, http.client.HTTPException):
        return b""


def _http_error_detail(body, headers):
    """` (message; Retry-After: N)` — the detail a non-200 carries beyond its status code.

    GitHub answers refusals with a JSON body (`{"message": …}`); the retry headers a client would
    honour are headers, not body. Both used to be discarded, so a rate-limited fetch refused as a
    bare `answered HTTP 403` with nothing to retry against.
    """
    parts = []
    text = body.decode("utf-8", "replace").strip() if body else ""
    if text:
        try:
            payload = json.loads(text)
        except ValueError:
            payload = None
        if isinstance(payload, dict) and isinstance(payload.get("message"), str):
            text = payload["message"]
        text = " ".join(text.split())[:300]
        parts.append(text)
    if headers is not None:
        for name in ("Retry-After", "X-RateLimit-Reset", "X-RateLimit-Remaining"):
            value = headers.get(name)
            if value:
                parts.append("%s: %s" % (name, value))
    return (" (%s)" % "; ".join(parts)) if parts else ""


def _http_status_refusal(url, status, body, headers):
    """The `Refusal` for a non-200 GET, shared by `listing` and `download`.

    Both callers of `_get` refuse a non-200 with the same message; assembling it in ONE place
    keeps the wording from drifting between them, the same consolidation `parse_lock_payload`
    gives the lock gate.
    """
    return Refusal(
        "fetch-fixtures: GET %s answered HTTP %s%s" % (url, status, _http_error_detail(body, headers))
    )


class GithubFixtureTransport(object):
    """The leg's transport: the GitHub contents API over `urllib`, each file from its `download_url`.

    The token (`GITHUB_TOKEN` / `GH_TOKEN`) goes to the API only, and only when set: the engine
    repos are public, so it lifts the anonymous rate limit and nothing else. Every listing names the
    ref it reads at in its own URL. Redirects never carry the Bearer token to a different host
    (`_NoCrossHostAuthRedirect`).
    """

    def __init__(self, token=None, urlopen=None, timeout=60):
        self.token = token
        self.timeout = timeout
        self._urlopen = urlopen or _OPENER.open

    def _get(self, url, api):
        headers = {"User-Agent": "chain-feed-fetch-fixtures"}
        if api:
            headers["Accept"] = "application/vnd.github+json"
            if self.token:
                headers["Authorization"] = "Bearer %s" % self.token
        request = urllib.request.Request(url, headers=headers)
        try:
            with self._urlopen(request, timeout=self.timeout) as response:
                return 200, response.read(), getattr(response, "headers", None)
        except urllib.error.HTTPError as exc:
            return exc.code, _http_error_body(exc), exc.headers
        except (urllib.error.URLError, OSError, http.client.HTTPException) as exc:
            raise Refusal("fetch-fixtures: GET %s failed: %s" % (url, exc))

    def listing(self, slug, path, sha):
        """The directory listing of `path` at `sha`, or `None` when there is no such directory."""
        url = "%s/repos/%s/contents/%s?ref=%s" % (GITHUB_API_URL, slug, path, sha)
        status, body, headers = self._get(url, api=True)
        if status == 404:
            return None
        if status != 200:
            raise _http_status_refusal(url, status, body, headers)
        try:
            data = json.loads(body.decode("utf-8"))
        except ValueError as exc:
            raise Refusal("fetch-fixtures: GET %s did not answer JSON: %s" % (url, exc))
        # A FILE where a directory was expected is not a fixture directory.
        return data if isinstance(data, list) else None

    def download(self, candidate):
        url = candidate.get("download_url") or ""
        if not str(url).startswith("https://"):
            raise Refusal("fetch-fixtures: %s has no https download_url (%r)" % (candidate["source"], url))
        status, body, headers = self._get(url, api=False)
        if status != 200:
            raise _http_status_refusal(url, status, body, headers)
        return body


def _listed(listing, kind, name=None):
    return [
        item for item in (listing or ())
        if isinstance(item, dict) and item.get("type") == kind and (name is None or item.get("name") == name)
    ]


def fixture_candidates(lock, transport):
    """`(candidates, missing)` — directory listings only, every one AT the lock's sha for its node.

    An engine node the lock does not carry contributes nothing. One the lock carries without a sha
    (`released`), or whose `tests/chain-fixtures/` holds no `<dir>/tools.jsonl` at that sha, is a
    named `missing` row — never an exception, never silently absent.
    """
    nodes = lock.get("nodes") or {}
    candidates = []
    missing = []

    def candidate(engine, node_id, slug, sha, path, item, reference):
        size = item.get("size")
        return {
            "engine": engine, "node": node_id, "slug": slug, "sha": sha, "path": path,
            "source": "%s@%s:%s" % (slug, sha[:8], path),
            "size": size if isinstance(size, int) and not isinstance(size, bool) else None,
            "download_url": item.get("download_url"),
            "reference": reference,
        }

    def absent(engine, node_id, sha, reason):
        missing.append({"engine": engine, "node": node_id, "sha": sha, "reason": reason})

    for node_id, engine in FIXTURE_ENGINES:
        if node_id not in nodes:
            continue
        entry = nodes.get(node_id) or {}
        sha = entry.get("sha")
        slug = RECIPES[node_id]["slug"]
        if not sha:
            absent(engine, node_id, None, "no fixture for %s: the lock gives %s no sha (state %r)"
                   % (engine, node_id, entry.get("state")))
            continue
        found = []
        directories = _listed(transport.listing(slug, FIXTURE_ROOT, sha), "dir")
        for directory in sorted(directories, key=lambda item: str(item.get("name"))):
            name = fixture_segment(directory.get("name"))
            if name is None:
                continue
            path = "%s/%s" % (FIXTURE_ROOT, name)
            for item in _listed(transport.listing(slug, path, sha), "file", FIXTURE_FILE):
                found.append(candidate(engine, node_id, slug, sha, "%s/%s" % (path, FIXTURE_FILE), item, False))
        if not found:
            absent(engine, node_id, sha, "no fixture for %s at %s" % (engine, sha[:8]))
        candidates.extend(found)

    if FIXTURE_REFERENCE_NODE in nodes:
        entry = nodes.get(FIXTURE_REFERENCE_NODE) or {}
        sha = entry.get("sha")
        slug = RECIPES[FIXTURE_REFERENCE_NODE]["slug"]
        engine = FIXTURE_REFERENCE_ENGINE
        if not sha:
            absent(engine, FIXTURE_REFERENCE_NODE, None,
                   "no reference fixture: the lock gives %s no sha (state %r)" % (FIXTURE_REFERENCE_NODE, entry.get("state")))
        else:
            path = "%s/%s" % (FIXTURE_ROOT, engine)
            items = _listed(transport.listing(slug, path, sha), "file", FIXTURE_FILE)
            if items:
                candidates.append(candidate(engine, FIXTURE_REFERENCE_NODE, slug, sha,
                                            "%s/%s" % (path, FIXTURE_FILE), items[0], True))
            else:
                absent(engine, FIXTURE_REFERENCE_NODE, sha, "no reference fixture for %s at %s" % (engine, sha[:8]))
    return candidates, missing


def fixture_meta(data):
    """The `meta` object on line 1 of a fixture. Tolerates a leading UTF-8 BOM (`_UTF8_BOM`) and a
    trailing CR (F1); raises `ValueError` on anything that is not a meta line."""
    if data.startswith(_UTF8_BOM):
        data = data[len(_UTF8_BOM):]
    meta = json.loads(data.decode("utf-8").split("\n", 1)[0].rstrip("\r"))
    if not isinstance(meta, dict) or meta.get("kind") != "meta":
        raise ValueError("line 1 is not a `kind: meta` object")
    return meta


def fetch_fixture_set(lock, out_dir, transport):
    """Fetch every candidate into `out_dir` and write `fixtures.json`; returns that manifest.

    A download whose length disagrees with the listing's `size` REFUSES the whole fetch (a
    truncated fixture must never be replayed). A file whose meta is unreadable, whose `schema` is not
    1, whose `engine` is not the node's, whose version/surface is not a safe path segment, or whose
    (engine, engine_version, surface) differs from an already-written fixture only by CASE is
    REFUSED BY NAME and never written — the other fixtures still land, and the caller exits 2.
    A leading UTF-8 BOM is stripped from the bytes written (F1 tolerates it on the wire; a consumer
    must never have to).
    """
    out_dir = Path(out_dir)
    candidates, missing = fixture_candidates(lock, transport)
    fixtures = []
    refused = []
    seen = {}
    case_seen = {}
    for item in candidates:
        data = transport.download(item)
        if item["size"] is not None and len(data) != item["size"]:
            raise Refusal(
                "fetch-fixtures: %s is %d bytes but its listing says %d — a truncated or altered "
                "fixture is never replayed" % (item["source"], len(data), item["size"])
            )
        if data.startswith(_UTF8_BOM):
            # A BOM is legal ON THE WIRE (F1: `fixture_meta` tolerates one) but must never reach
            # the layout: a consumer json-parses line 1, and the BOM fails that parse with a
            # message that never names the BOM. Strip it here, visibly, before meta and write.
            data = data[len(_UTF8_BOM):]

        def refuse(reason, item=item):
            refused.append({"engine": item["engine"], "node": item["node"], "sha": item["sha"],
                            "source": item["source"], "reason": reason})

        try:
            meta = fixture_meta(data)
        except ValueError as exc:
            refuse("%s: line 1 is not a readable meta line (%s)" % (item["source"], exc))
            continue
        if meta.get("schema") != FIXTURE_SCHEMA or isinstance(meta.get("schema"), bool):
            refuse("%s: meta.schema is %r, not %d — a fixture of another schema is never replayed"
                   % (item["source"], meta.get("schema"), FIXTURE_SCHEMA))
            continue
        if meta.get("engine") != item["engine"]:
            refuse("%s: meta.engine is %r, not %r" % (item["source"], meta.get("engine"), item["engine"]))
            continue
        version = fixture_segment(meta.get("engine_version"))
        surface = fixture_segment(meta.get("surface"))
        if version is None or surface is None:
            refuse("%s: meta.engine_version %r / meta.surface %r is not a safe path segment"
                   % (item["source"], meta.get("engine_version"), meta.get("surface")))
            continue
        key = (item["engine"], version, surface)
        if key in seen:
            refuse("%s: the same (engine, engine_version, surface) %r as %s" % (item["source"], key, seen[key]))
            continue
        fold_key = (key[0].lower(), key[1].lower(), key[2].lower())
        other = case_seen.get(fold_key)
        if other is not None:
            refuse(
                "%s: its layout path %s/%s/%s differs only by case from %s — on Windows both fold "
                "onto ONE file, so this one is never written and the manifest never disagrees "
                "with disk" % (item["source"], item["engine"], version, surface, other)
            )
            continue
        seen[key] = item["source"]
        case_seen[fold_key] = item["source"]
        target = out_dir / item["engine"] / version / surface / FIXTURE_FILE
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        fixtures.append({
            "engine": item["engine"], "engine_version": version, "surface": surface,
            "path": as_posix_abs(target), "sha256": sha256_bytes(data), "bytes": len(data),
            "node": item["node"], "sha": item["sha"], "source": item["source"],
            "reference": item["reference"],
        })
    manifest = {
        "schema": 1,
        "lock_hash": str(lock.get("lock_hash") or ""),
        "env": REPLAY_ENV,
        "skip_name": REPLAY_SKIP_NAME,
        "fixtures": fixtures,
        "missing": missing,
        "refused": refused,
    }
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / FIXTURES_MANIFEST).write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return manifest


def fixture_summary(manifest):
    """`replay: fetched N fixture(s) (unity 3 @<sha8>, …) + null-engine reference @<sha8>`, then one
    line per missing / refused row."""
    counts = []
    for node_id, engine in FIXTURE_ENGINES:
        rows = [f for f in manifest["fixtures"] if f["engine"] == engine and not f["reference"]]
        if rows:
            counts.append("%s %d @%s" % (engine, len(rows), rows[0]["sha"][:8]))
    total = sum(1 for f in manifest["fixtures"] if not f["reference"])
    line = "replay: fetched %d fixture(s) (%s)" % (total, ", ".join(counts) or "none")
    for reference in [f for f in manifest["fixtures"] if f["reference"]]:
        line += " + %s reference @%s" % (reference["engine"], reference["sha"][:8])
    lines = [line]
    lines.extend("replay: missing: %s" % row["reason"] for row in manifest["missing"])
    lines.extend("replay: REFUSED: %s" % row["reason"] for row in manifest["refused"])
    return "\n".join(lines)


@command("fetch-fixtures", "fetch the T2 replay fixtures at the lock's engine shas (p2-replay-leg)")
def cmd_fetch_fixtures(args):
    lock_text, _hash8, source = load_lock_text(args)
    if lock_text is None:
        log("chain: no lock (ordinary run)")
        return EXIT_OK
    lock = parse_lock_payload(lock_text, source)
    out = Path(getattr(args, "out", None) or (_default_runner_temp() / "chain-t2"))
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN") or None
    manifest = fetch_fixture_set(lock, out, GithubFixtureTransport(token=token))
    for line in fixture_summary(manifest).splitlines():
        log(line)
    log("chain: fixture manifest -> %s" % as_posix_abs(out / FIXTURES_MANIFEST))
    _write_github_env([(REPLAY_ENV, as_posix_abs(out))])
    _write_github_output([
        ("fixtures", str(len(manifest["fixtures"]))),
        ("missing", str(len(manifest["missing"]))),
        ("refused", str(len(manifest["refused"]))),
    ])
    return EXIT_REFUSED if manifest["refused"] else EXIT_OK


# ----------------------------------------------------------------------
# CLI
# ----------------------------------------------------------------------


def build_parser():
    parser = argparse.ArgumentParser(
        prog="chain_feed.py",
        description="chain leg feed: self-build the lower layers from the lock and prove identity",
    )
    sub = parser.add_subparsers(dest="command")
    for name in sorted(COMMANDS):
        function, help_text = COMMANDS[name]
        child = sub.add_parser(name, help=help_text)
        child.add_argument("--node", help="this repo's canonical node id")
        child.add_argument("--job", default="", help="the workflow job key (leg record + artifact name)")
        child.add_argument("--lock", default=None, help="lock JSON (default: $CHAIN_LOCK, then the PR body)")
        child.add_argument("--lock-file", dest="lock_file", default=None, help="read the lock from a file")
        child.add_argument("--checkout", default=None, help="repo checkout root (default: $GITHUB_WORKSPACE)")
        child.add_argument("--runner-temp", dest="runner_temp", default=None, help="default: $RUNNER_TEMP")
        if name in ("apply", "dry-run", "record"):
            child.add_argument(
                "--edges", action="append", default=[], metavar="SELECTOR",
                help="scope THIS job to part of the node's edges: a mode (nuget), a producer node id, "
                     "a <node>/<artifact> ref, `self` (this node's own pack), `twin:<node>` (clone a "
                     "declared twin at the lock sha; never implied) or `none`; repeatable. "
                     "Pass the SAME values to apply and record; default: every edge",
            )
        if name == "apply-npm":
            child.add_argument("--dir", default=".", help="the CLI directory to install into")
        if name == "record":
            child.add_argument("--out", default="chain-leg.json")
            child.add_argument("--matrix", default="", help="matrix suffix for the artifact name")
            child.add_argument("--engine", default="")
            child.add_argument("--engine-version", dest="engine_version", default="")
            child.add_argument("--tier", default="T0")
            child.add_argument("--job-status", dest="job_status", default="")
            child.add_argument("--skip", action="append", default=[], metavar="STEP=REASON")
            child.add_argument(
                "--warn", action="append", default=[], metavar="TEXT",
                help="add TEXT to the record's warnings[] (repeatable); never changes the result",
            )
        if name == "fetch-fixtures":
            child.add_argument(
                "--out", default=None,
                help="directory for <engine>/<engine_version>/<surface>/tools.jsonl + fixtures.json "
                     "(default: $RUNNER_TEMP/chain-t2); exported as CHAIN_REPLAY_FIXTURES",
            )
        if name == "print-fragment":
            child.add_argument("--existing-group", dest="existing_group", default=None)
            child.add_argument("--existing-cancel", dest="existing_cancel", default=None)
            child.add_argument("--new-concurrency", dest="new_concurrency", action="store_true")
        child.set_defaults(func=function)
    return parser


def _configure_stdio():
    for stream in (sys.stdout, sys.stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            try:
                reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):  # pragma: no cover
                pass


def main(argv=None):
    _configure_stdio()
    parser = build_parser()
    args = parser.parse_args(list(argv) if argv is not None else None)
    if not getattr(args, "func", None):
        parser.print_help()
        return EXIT_REFUSED
    try:
        return int(args.func(args))
    except Refusal as exc:
        print("chain: refused: %s" % exc, file=sys.stderr)
        return EXIT_REFUSED
    except OSError as exc:
        print("chain: refused: %s" % exc, file=sys.stderr)
        return EXIT_REFUSED


if __name__ == "__main__":
    raise SystemExit(main())
