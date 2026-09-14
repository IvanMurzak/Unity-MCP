/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.McpPlugin.Common.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ContentType = com.IvanMurzak.McpPlugin.Common.Consts.ContentType;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Chain
{
    /// <summary>
    /// T2 chain recorder (IN-PROCESS): dumps the editor's own tool contract — <c>tools/list</c> plus
    /// one <c>tools/call</c> per battery entry — as an F1-shaped raw dump, the format defined by
    /// MCP-Plugin-dotnet <c>docs/chain-fixtures.md</c>. The CI leg then runs
    /// <c>.github/scripts/chain_fixture.py canonicalize</c> + <c>diff</c> against
    /// <c>tests/chain-fixtures/&lt;unity-version&gt;/tools.jsonl</c>.
    ///
    /// <para>Active ONLY when the editor was started with <c>-CHAIN_RECORD_OUT &lt;path&gt;</c> (and
    /// <c>-CHAIN_BATTERY &lt;path&gt;</c>); otherwise it is a named skip, so an ordinary Test Runner
    /// run is unaffected. Relative paths resolve against the Unity PROJECT root, never the process
    /// working directory, so the same arguments mean the same files inside the game-ci container
    /// and on a developer machine.</para>
    ///
    /// <para><b>The response projection mirrors the server</b>, as MCP-Plugin-dotnet's reference
    /// recorder <c>McpPlugin.NullEngine/src/Replay/RawDump.cs</c> does: an outer error discards the
    /// response's own content and becomes one text block; a null value becomes a fixed text; a text
    /// block loses its MimeType. What is recorded is what an MCP client observes. The serialisation
    /// uses only the EXISTING pinned <c>ResponseListTool</c> / <c>ResponseCallTool</c> types and
    /// <c>System.Text.Json</c> — no new plugin API, so this compiles against the pinned DLLs.</para>
    /// </summary>
    public class ChainRecordTests
    {
        const string ArgRecordOut = "CHAIN_RECORD_OUT";
        const string ArgBattery = "CHAIN_BATTERY";
        const int FixtureSchema = 1;
        const float TimeoutSeconds = 300f;

        [UnityTest]
        public IEnumerator RecordEditorToolContract()
        {
            var args = ArgsUtils.ParseCommandLineArguments();
            var outArg = Clean(args.GetValueOrDefault(ArgRecordOut));
            if (string.IsNullOrEmpty(outArg))
                Assert.Ignore("chain record not requested");

            // The battery records ERROR contracts on purpose (e.g. an unknown tool), and the tool manager
            // logs every such error. Those logs are the recorded behaviour, not a test failure; whether the
            // recording is right is decided by the fixture diff on the host, not by the log stream.
            LogAssert.ignoreFailingMessages = true;

            var batteryArg = Clean(args.GetValueOrDefault(ArgBattery));
            Assert.IsFalse(string.IsNullOrEmpty(batteryArg),
                $"-{ArgRecordOut} was given without -{ArgBattery} <path>: the recorder needs a battery.");

            // Unity API values are captured HERE, on the main thread; the recording itself runs on
            // the thread pool so tools that marshal onto the main thread cannot deadlock on it.
            var projectRoot = Path.GetDirectoryName(Application.dataPath)!;
            var outPath = ResolvePath(projectRoot, outArg!);
            var batteryPath = ResolvePath(projectRoot, batteryArg!);
            var engineVersion = Application.unityVersion;

            UnityMcpPluginEditor.InitSingletonIfNeeded();
            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "The MCP plugin was built but exposes no tool manager.");

            var task = Task.Run(() => RecordAsync(toolManager!, outPath, batteryPath, engineVersion));

            var startTime = Time.realtimeSinceStartup;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup - startTime > TimeoutSeconds)
                    Assert.Fail($"chain-record did not finish within {TimeoutSeconds} seconds.");
                yield return null;
            }
            if (task.IsFaulted)
                ExceptionDispatchInfo.Capture(task.Exception!.GetBaseException()).Throw();

            var (tools, calls) = task.Result;
            Debug.Log($"chain-record: {tools} tools, {calls} calls → {Path.GetFileName(outPath)} ({outPath})");

            Assert.IsTrue(File.Exists(outPath), $"chain-record did not write {outPath}");
            Assert.Greater(tools, 0, "chain-record listed zero tools.");
        }

        static string? Clean(string? value) => value?.Trim().Trim('"');

        static string ResolvePath(string projectRoot, string path)
            => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path));

        static async Task<(int tools, int calls)> RecordAsync(IToolManager toolManager, string outPath, string batteryPath, string engineVersion)
        {
            var battery = LoadBattery(batteryPath);
            var lines = new List<string>();

            var meta = new JsonObject
            {
                ["schema"] = FixtureSchema,
                ["kind"] = "meta",
                ["engine"] = "unity",
                ["engine_version"] = engineVersion,
                ["surface"] = "editor",
                ["plugin_version"] = UnityMcpPlugin.Version,
                ["mcp_plugin_version"] = InformationalVersion(typeof(IToolManager).Assembly),
                ["recorded_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                ["recorder"] = "in-process"
            };
            lines.Add(meta.ToJsonString());

            var listed = await toolManager.RunListTool(new RequestListTool()).ConfigureAwait(false);
            if (listed.Status == ResponseStatus.Error || listed.Value == null)
                throw new InvalidOperationException("tools/list failed in-process: " + (listed.Message ?? "no message"));

            foreach (var tool in listed.Value)
                lines.Add(ToolLine(tool).ToJsonString());

            foreach (var (name, callArgs) in battery)
            {
                var outer = await toolManager
                    .RunCallTool(new RequestCallTool(name, ToArguments(callArgs)))
                    .ConfigureAwait(false);

                var line = new JsonObject
                {
                    ["kind"] = "call",
                    ["name"] = name,
                    ["args"] = callArgs,
                    ["response"] = ProjectResponse(outer)
                };
                lines.Add(line.ToJsonString());
            }

            // F1: UTF-8 without a BOM, LF-separated. Canonicalisation (sorting, masking, caps and
            // args_hash) is chain_fixture.py's job, on the host.
            var directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var builder = new StringBuilder();
            foreach (var text in lines)
                builder.Append(text).Append('\n');
            File.WriteAllText(outPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return (listed.Value.Length, battery.Count);
        }

        static List<(string name, JsonObject args)> LoadBattery(string path)
        {
            var document = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) as JsonObject
                ?? throw new InvalidOperationException($"battery {path} must be a JSON object");

            var schema = document["schema"]?.ToJsonString();
            if (schema != FixtureSchema.ToString(CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"battery {path}: schema mismatch - expected {FixtureSchema}, got {schema ?? "<missing>"}");

            if (document["calls"] is not JsonArray calls || calls.Count == 0)
                throw new InvalidOperationException($"battery {path} must carry a non-empty 'calls' array");

            var result = new List<(string, JsonObject)>();
            foreach (var item in calls)
            {
                var name = (item as JsonObject)?["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(name))
                    throw new InvalidOperationException($"battery {path}: every call needs a 'name'");

                // Detached copy: a node can have only one parent, and the battery document owns this one.
                var args = item!["args"] is JsonObject source
                    ? (JsonObject)JsonNode.Parse(source.ToJsonString())!
                    : new JsonObject();
                result.Add((name!, args));
            }
            return result;
        }

        static IReadOnlyDictionary<string, JsonElement> ToArguments(JsonObject args)
        {
            var arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var pair in args)
            {
                using var document = JsonDocument.Parse(pair.Value?.ToJsonString() ?? "null");
                arguments[pair.Key] = document.RootElement.Clone();
            }
            return arguments;
        }

        static string InformationalVersion(Assembly assembly)
            => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "unknown";

        /// <summary>Mirror of RawDump.ToolLine: the ResponseListTool fields that reach an MCP client.</summary>
        static JsonObject ToolLine(ResponseListTool tool)
        {
            var line = new JsonObject
            {
                ["kind"] = "tool",
                ["name"] = tool.Name
            };
            if (!string.IsNullOrEmpty(tool.Title))
                line["title"] = tool.Title;
            if (!string.IsNullOrEmpty(tool.Description))
                line["description"] = tool.Description;
            if (tool.InputSchema.ValueKind != JsonValueKind.Undefined)
                line["inputSchema"] = JsonNode.Parse(tool.InputSchema.GetRawText());
            if (tool.OutputSchema.HasValue && tool.OutputSchema.Value.ValueKind != JsonValueKind.Undefined)
                line["outputSchema"] = JsonNode.Parse(tool.OutputSchema.Value.GetRawText());
            if (tool.ReadOnlyHint.HasValue)
                line["readOnlyHint"] = tool.ReadOnlyHint.Value;
            if (tool.DestructiveHint.HasValue)
                line["destructiveHint"] = tool.DestructiveHint.Value;
            if (tool.IdempotentHint.HasValue)
                line["idempotentHint"] = tool.IdempotentHint.Value;
            if (tool.OpenWorldHint.HasValue)
                line["openWorldHint"] = tool.OpenWorldHint.Value;

            // Deliberately NOT recorded (format §F1): Enabled and the skill metadata, which an
            // MCP-client capture can never observe.
            return line;
        }

        /// <summary>Mirror of RawDump.ProjectResponse — ToolRouter.Call's order: packed error, null value, mapping.</summary>
        static JsonObject ProjectResponse(ResponseData<ResponseCallTool> outer)
        {
            if (outer.Status == ResponseStatus.Error)
                return ErrorEnvelope(outer.Message ?? "[Error] Got an error during running tool", outer.ErrorKind);

            if (outer.Value == null)
                return ErrorEnvelope("[Error] Tool returned null value", outer.ErrorKind);

            var value = outer.Value;
            var content = new JsonArray();
            if (value.Content != null)
            {
                foreach (var block in value.Content)
                    content.Add(ToContent(block));
            }

            var response = new JsonObject
            {
                ["status"] = value.Status == ResponseStatus.Error ? "error" : "success",
                ["content"] = content
            };
            if (value.StructuredContent != null)
                response["structuredContent"] = JsonNode.Parse(value.StructuredContent.ToJsonString());
            response["errorKind"] = outer.ErrorKind.ToString();
            return response;
        }

        static JsonObject ErrorEnvelope(string message, ResponseErrorKind errorKind) => new JsonObject
        {
            ["status"] = "error",
            ["content"] = new JsonArray(new JsonObject
            {
                ["type"] = ContentType.Text,
                ["text"] = message
            }),
            ["errorKind"] = errorKind.ToString()
        };

        /// <summary>Mirror of ExtensionsContentBlock.ToContent, block kind by block kind.</summary>
        static JsonObject ToContent(ContentBlock block)
        {
            switch (block.Type)
            {
                case ContentType.Image:
                case ContentType.Audio:
                    return new JsonObject
                    {
                        ["type"] = block.Type,
                        ["data"] = block.Data ?? string.Empty,
                        ["mimeType"] = block.MimeType ?? string.Empty
                    };

                case ContentType.Resource:
                    var resource = new JsonObject { ["uri"] = block.Resource?.Uri ?? string.Empty };
                    if (block.Resource?.MimeType != null)
                        resource["mimeType"] = block.Resource.MimeType;
                    // Text is preferred over Blob, as on the wire.
                    if (block.Resource?.Text != null)
                        resource["text"] = block.Resource.Text;
                    else if (block.Resource?.Blob != null)
                        resource["blob"] = block.Resource.Blob;
                    return new JsonObject
                    {
                        ["type"] = ContentType.Resource,
                        ["resource"] = resource
                    };

                default:
                    // A text block LOSES its MimeType on the wire.
                    return new JsonObject
                    {
                        ["type"] = ContentType.Text,
                        ["text"] = block.Text ?? string.Empty
                    };
            }
        }
    }
}
