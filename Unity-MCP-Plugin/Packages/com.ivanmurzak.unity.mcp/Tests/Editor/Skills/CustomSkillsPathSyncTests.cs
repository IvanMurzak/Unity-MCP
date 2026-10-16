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
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.McpPlugin.AgentConfig.Impl;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression coverage for issue #1023: after a domain reload the shared
    /// <see cref="CustomConfigurator"/> is back to its <c>".claude/skills"</c> default, and
    /// Startup / the window copied that default over the user's saved custom skills path.
    /// </summary>
    [TestFixture]
    public class CustomSkillsPathSyncTests
    {
        const string DefaultPath = ".claude/skills";
        const string CustomPath = "my-custom/skills";

        private string _originalSkillsPath = null!;
        private CustomConfigurator _custom = null!;
        private string _originalEditablePath = null!;

        [SetUp]
        public void SetUp()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            _originalSkillsPath = UnityMcpPluginEditor.SkillsPath;

            _custom = (CustomConfigurator)AiAgentConfiguratorRegistry.GetByAgentId("other-custom")!;
            Assert.IsNotNull(_custom, "The shared registry must expose the Custom configurator.");
            _originalEditablePath = _custom.EditableSkillsPath;
        }

        [TearDown]
        public void TearDown()
        {
            _custom.EditableSkillsPath = _originalEditablePath;
            UnityMcpPluginEditor.SkillsPath = _originalSkillsPath;
        }

        [Test]
        public void SyncCustomSkillsPath_SeedsCustomConfiguratorFromSavedPath()
        {
            UnityMcpPluginEditor.SkillsPath = CustomPath;
            _custom.EditableSkillsPath = DefaultPath; // state after a domain reload

            UnityMcpPluginEditor.SyncCustomSkillsPath(_custom);

            Assert.AreEqual(CustomPath, _custom.SkillsPath);
        }

        [Test]
        public void SyncCustomSkillsPath_ThenApplyingAgentPath_KeepsSavedPath()
        {
            UnityMcpPluginEditor.SkillsPath = CustomPath;
            _custom.EditableSkillsPath = DefaultPath;

            // Mirrors Startup / MainWindowEditor.LoadAgentUI with auto-generate enabled.
            UnityMcpPluginEditor.SyncCustomSkillsPath(_custom);
            UnityMcpPluginEditor.SkillsPath = _custom.SkillsPath!;

            Assert.AreEqual(CustomPath, UnityMcpPluginEditor.SkillsPath);
        }

        [Test]
        public void SyncCustomSkillsPath_IgnoresBuiltInAgents()
        {
            UnityMcpPluginEditor.SkillsPath = CustomPath;
            var builtIn = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            Assume.That(builtIn, Is.Not.Null.And.Not.InstanceOf<CustomConfigurator>());
            var builtInPath = builtIn!.SkillsPath;

            UnityMcpPluginEditor.SyncCustomSkillsPath(builtIn);

            Assert.AreEqual(builtInPath, builtIn.SkillsPath);
            Assert.AreEqual(CustomPath, UnityMcpPluginEditor.SkillsPath);
        }
    }
}
