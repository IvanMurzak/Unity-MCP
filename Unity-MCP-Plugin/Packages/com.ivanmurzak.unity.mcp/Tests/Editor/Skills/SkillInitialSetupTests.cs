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
using com.IvanMurzak.Unity.MCP.Editor.API;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class SkillInitialSetupTests
    {
        [Test]
        public void SkillId_ShouldBe_UnityInitialSetup()
        {
            Assert.AreEqual("unity-initial-setup", Skill_InitialSetup.SkillId);
        }

        [Test]
        public void Markdown_ShouldNotBeNullOrEmpty()
        {
            var markdown = Skill_InitialSetup.Markdown;

            Assert.IsNotNull(markdown);
            Assert.IsNotEmpty(markdown);
        }

        [Test]
        public void Markdown_ShouldContain_NodejsInstallInstructions()
        {
            var markdown = Skill_InitialSetup.Markdown;

            Assert.That(markdown, Does.Contain("Node.js"));
            Assert.That(markdown, Does.Contain("npm install -g unity-mcp-cli"));
        }

        [Test]
        public void Markdown_ShouldContain_CliCommands()
        {
            var markdown = Skill_InitialSetup.Markdown;

            Assert.That(markdown, Does.Contain("unity-mcp-cli install-plugin"));
            Assert.That(markdown, Does.Contain("unity-mcp-cli configure"));
            Assert.That(markdown, Does.Contain("unity-mcp-cli setup-mcp"));
            Assert.That(markdown, Does.Contain("unity-mcp-cli setup-skills"));
            Assert.That(markdown, Does.Contain("unity-mcp-cli open"));
        }

        [Test]
        public void Markdown_ShouldContain_VersionRequirement()
        {
            var markdown = Skill_InitialSetup.Markdown;

            Assert.That(markdown, Does.Contain("20.19.0"));
            Assert.That(markdown, Does.Contain("22.12.0"));
        }

        [Test]
        public void Markdown_ShouldContain_ExactlyOnePlatformInstallInstructions()
        {
            var markdown = Skill_InitialSetup.Markdown;

            // Should contain exactly one of these platform-specific instructions
            var hasWinget = markdown.Contains("winget install");
            var hasBrew = markdown.Contains("brew install");
            var hasAptGet = markdown.Contains("apt-get install");

            var platformCount = (hasWinget ? 1 : 0) + (hasBrew ? 1 : 0) + (hasAptGet ? 1 : 0);
            Assert.AreEqual(1, platformCount,
                $"Expected exactly one platform-specific instruction. winget={hasWinget}, brew={hasBrew}, apt-get={hasAptGet}");
        }

        [Test]
        public void Markdown_ShouldContain_TroubleshootingSection()
        {
            var markdown = Skill_InitialSetup.Markdown;

            Assert.That(markdown, Does.Contain("Troubleshooting"));
            Assert.That(markdown, Does.Contain("npm"));
        }
    }
}
