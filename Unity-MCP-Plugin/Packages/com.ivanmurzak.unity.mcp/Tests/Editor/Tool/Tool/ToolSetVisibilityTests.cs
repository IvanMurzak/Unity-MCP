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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// <c>tool-set-visibility</c> (the renamed <c>tool-set-enabled-state</c>): it only decides whether a tool is listed to MCP
    /// clients; a hidden tool still runs when called by name. The old name keeps working (ToolSetEnabledStateTests).
    /// </summary>
    [TestFixture]
    public class ToolSetVisibilityTests : BaseTest
    {
        const string HiddenProbeToolId = Tool_Tool.ToolListId;
        readonly Dictionary<string, bool> _original = new();

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");
            foreach (var id in new[] { HiddenProbeToolId, Tool_Tool.ToolSetVisibilityId, Tool_Tool.ToolSetEnabledStateId })
            {
                _original[id] = toolManager!.IsToolVisible(id);
                toolManager.SetToolVisible(id, true);
            }
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null)
            {
                foreach (var kvp in _original)
                    toolManager.SetToolVisible(kvp.Key, kvp.Value);
                UnityMcpPluginEditor.Instance.Save();
            }
            _original.Clear();

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator SetVisibility_HidesAndShowsATool()
        {
            yield return null;
            var toolManager = UnityMcpPluginEditor.Instance.Tools!;

            var json = RunTool(Tool_Tool.ToolSetVisibilityId, $@"{{
                ""tools"": [{{ ""name"": ""{HiddenProbeToolId}"", ""visible"": false }}]
            }}").Value!.ToolResultJson();
            Assert.IsTrue(ToolSetEnabledStateTests.GetSuccessValue(json, HiddenProbeToolId));
            Assert.IsFalse(toolManager.IsToolVisible(HiddenProbeToolId), "the tool should be hidden");

            json = RunTool(Tool_Tool.ToolSetVisibilityId, $@"{{
                ""tools"": [{{ ""name"": ""{HiddenProbeToolId}"", ""visible"": true }}]
            }}").Value!.ToolResultJson();
            Assert.IsTrue(ToolSetEnabledStateTests.GetSuccessValue(json, HiddenProbeToolId));
            Assert.IsTrue(toolManager.IsToolVisible(HiddenProbeToolId), "the tool should be visible again");
        }

        [UnityTest]
        public IEnumerator HiddenTool_StillRunsWhenCalledByName()
        {
            yield return null;
            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            toolManager.SetToolVisible(HiddenProbeToolId, false);

            var result = RunTool(HiddenProbeToolId, @"{}");

            Assert.IsFalse(toolManager.IsToolVisible(HiddenProbeToolId));
            Assert.IsNotNull(result.Value, "a hidden tool must still answer a call");
            Assert.AreEqual(ResponseStatus.Success, result.Value!.Status, "a hidden tool must still run");
        }

        [UnityTest]
        public IEnumerator DeprecatedName_HidesTheSameWay()
        {
            yield return null;
            var toolManager = UnityMcpPluginEditor.Instance.Tools!;

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{HiddenProbeToolId}"", ""enabled"": false }}]
            }}").Value!.ToolResultJson();

            Assert.IsTrue(ToolSetEnabledStateTests.GetSuccessValue(json, HiddenProbeToolId));
            Assert.IsFalse(toolManager.IsToolVisible(HiddenProbeToolId), "the old tool name must still hide the tool");
        }

        [UnityTest]
        public IEnumerator BothNames_AreRegistered_AndHiddenFromToolsListByDefault()
        {
            yield return null;
            var names = UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Select(t => t.Name).ToList();
            Assert.Contains(Tool_Tool.ToolSetVisibilityId, names);
            Assert.Contains(Tool_Tool.ToolSetEnabledStateId, names);
        }
    }
}
