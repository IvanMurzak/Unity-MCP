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
    /// The persisted <see cref="UnityMcpPluginEditor.SkillsPath"/> is now mirrored onto it.
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
            // The setter mirrors onto the Custom configurator, so restore it first and the
            // configurator's own value last, or the mirror clobbers the restore.
            UnityMcpPluginEditor.SkillsPath = _originalSkillsPath;
            _custom.EditableSkillsPath = _originalEditablePath;
        }

        [Test]
        public void SkillsPathSetter_MirrorsSavedPathOntoCustomConfigurator()
        {
            _custom.EditableSkillsPath = DefaultPath; // state after a domain reload

            UnityMcpPluginEditor.SkillsPath = CustomPath;

            // Startup / MainWindowEditor.LoadAgentUI copy configurator.SkillsPath back into the
            // persisted value; for Custom that must now be the saved path, not the default.
            Assert.AreEqual(CustomPath, _custom.SkillsPath);
        }

        [Test]
        public void SkillsPathSetter_LeavesBuiltInAgentsUntouched()
        {
            var builtIn = AiAgentConfiguratorRegistry.GetByAgentId("claude-code");
            Assume.That(builtIn, Is.Not.Null.And.Not.InstanceOf<CustomConfigurator>());
            var builtInPath = builtIn!.SkillsPath;

            UnityMcpPluginEditor.SkillsPath = CustomPath;

            Assert.AreEqual(builtInPath, builtIn.SkillsPath);
        }
    }
}
