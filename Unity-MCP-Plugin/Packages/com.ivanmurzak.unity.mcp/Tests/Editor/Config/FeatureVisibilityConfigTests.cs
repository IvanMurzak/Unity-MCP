/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The config file's tool / prompt / resource entries are written as <c>"visible"</c>; a file saved before the rename
    /// (<c>"enabled"</c>) still loads with its hidden items, and <c>"visible"</c> wins when both are present.
    /// </summary>
    public class FeatureVisibilityConfigTests
    {
        // The production serializer options (Editor/Scripts/UnityMcpPluginEditor.Config.cs: Save / GetOrCreateConfig).
        static JsonSerializerOptions SaveOptions() => new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        static JsonSerializerOptions LoadOptions() => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        static UnityMcpPlugin.UnityConnectionConfig Load(string json)
            => JsonSerializer.Deserialize<UnityMcpPlugin.UnityConnectionConfig>(json, LoadOptions())!;

        [Test]
        public void AConfigSavedBeforeTheRename_KeepsItsHiddenTool()
        {
            var config = Load(@"{ ""tools"": [ { ""name"": ""script-read"", ""enabled"": false }, { ""name"": ""ping"", ""enabled"": true } ] }");

            Assert.IsFalse(config.Tools.Single(t => t.Name == "script-read").Visible);
            Assert.IsTrue(config.Tools.Single(t => t.Name == "ping").Visible);
        }

        [Test]
        public void SavingWritesVisible_AndNeverTheOldKey()
        {
            var config = Load(@"{ ""tools"": [ { ""name"": ""script-read"", ""enabled"": false } ] }");

            var json = JsonSerializer.Serialize(config, SaveOptions());

            StringAssert.Contains(@"""visible"": false", json);
            StringAssert.DoesNotContain(@"""enabled""", json);
            Assert.IsFalse(Load(json).Tools.Single().Visible);
        }

        [Test]
        public void TheToolsPromptsResourcesWindows_FilterAllVisibleHidden()
        {
            // The windows' filter dropdown lists these names (McpListWindowBase.InitializeFilters).
            CollectionAssert.AreEqual(new[] { "All", "Visible", "Hidden" },
                System.Enum.GetNames(typeof(com.IvanMurzak.Unity.MCP.Editor.UI.McpFilterType)));
        }

        [Test]
        public void BothKeys_TakeVisible()
        {
            var config = Load(@"{ ""prompts"": [ { ""name"": ""p"", ""enabled"": true, ""visible"": false } ] }");

            Assert.IsFalse(config.Prompts.Single().Visible);
        }
    }
}
