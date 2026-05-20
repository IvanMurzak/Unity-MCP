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
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression coverage for issue #766: the McpPlugin 6.3.1 upgrade introduced an
    /// <see cref="System.InvalidOperationException"/> at <see cref="com.IvanMurzak.McpPlugin.McpPlugin"/>
    /// construction time whenever <c>ConnectionConfig.ProjectRootPath</c> is <c>null</c> and the
    /// auto-fire skill-generation path is invoked with a relative <c>SkillsPath</c>.
    ///
    /// The Unity-MCP host now sets <see cref="UnityMcpPlugin.UnityConnectionConfig.ProjectRootPath"/>
    /// in <see cref="UnityMcpPluginEditor.BuildMcpPluginIfNeeded"/> — once per build cycle, before
    /// the underlying <see cref="com.IvanMurzak.McpPlugin.McpPlugin"/> is materialised.
    ///
    /// See also: https://github.com/IvanMurzak/Unity-MCP/issues/766,
    /// https://github.com/IvanMurzak/MCP-Plugin-dotnet/pull/108.
    /// </summary>
    [TestFixture]
    public class BuildMcpPluginIfNeededProjectRootTests
    {
        private string? _originalProjectRootPath;

        [SetUp]
        public void SetUp()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            // Snapshot the field so unrelated tests / Editor state are unaffected by this
            // fixture's mutation. Mirrors the pattern used by SkillsPathNormalizationTests
            // for the SkillsPath field in the same folder.
            _originalProjectRootPath = UnityMcpPluginEditor.Instance.ConnectionConfigForTests.ProjectRootPath;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore so unrelated tests / Editor state are unaffected.
            UnityMcpPluginEditor.Instance.ConnectionConfigForTests.ProjectRootPath = _originalProjectRootPath;
        }

        [Test]
        public void BuildMcpPluginIfNeeded_SetsProjectRootPath_OnConnectionConfig()
        {
            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();

            var config = UnityMcpPluginEditor.Instance.ConnectionConfigForTests;

            Assert.AreEqual(
                UnityMcpPluginEditor.ProjectRootPath,
                config.ProjectRootPath,
                "ConnectionConfig.ProjectRootPath must be set to UnityMcpPluginEditor.ProjectRootPath " +
                "so the McpPlugin auto-fire skill-generation path does not throw InvalidOperationException.");

            // Guard the regression contract directly: the whole point of seeding
            // ProjectRootPath before _plugin.BuildOnce is to prevent McpPlugin's ctor
            // from logging InvalidOperationException via the relative-SkillsPath path.
            // A future refactor that moved the assignment AFTER BuildOnce would still
            // pass the equality assert above (because the field is set by the time we
            // observe it), but McpPlugin would have logged the exception during the
            // build. NoUnexpectedReceived() catches that regression — any uncaught log
            // during the test body fails the test here.
            LogAssert.NoUnexpectedReceived();
        }
    }
}
