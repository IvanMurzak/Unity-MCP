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
using System.IO;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="UnityMcpPluginEditor.NormalizeSkillsPath"/> and the
    /// <see cref="UnityMcpPluginEditor.SkillsPath"/> setter's persistence behaviour.
    ///
    /// Regression coverage for issue #761: `skillsPath` persisted as an absolute path
    /// breaks shared `UserSettings/AI-Game-Developer-Config.json` across machines.
    /// The setter now normalises absolute-inside-project paths to project-relative
    /// form (with forward slashes), and the load-time path in
    /// <see cref="UnityMcpPluginEditor.GetOrCreateConfig"/> auto-heals legacy values.
    /// </summary>
    [TestFixture]
    public class SkillsPathNormalizationTests
    {
        private string _originalSkillsPath = null!;

        [SetUp]
        public void SetUp()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            _originalSkillsPath = UnityMcpPluginEditor.SkillsPath;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore so unrelated tests / Editor state are unaffected.
            UnityMcpPluginEditor.SkillsPath = _originalSkillsPath;
        }

        // ----- NormalizeSkillsPath pure-function tests --------------------------------

        [Test]
        public void NormalizeSkillsPath_KeepsRelativeValue()
        {
            // Relative path stays relative.
            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath(".claude/skills");
            Assert.AreEqual(".claude/skills", normalized);
        }

        [Test]
        public void NormalizeSkillsPath_KeepsRelativeNestedValue()
        {
            // Multi-segment relative paths survive.
            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath("nested/sub/skills");
            Assert.AreEqual("nested/sub/skills", normalized);
        }

        [Test]
        public void NormalizeSkillsPath_NormalizesBackslashesToForwardSlashes()
        {
            // Cross-platform diff stability — backslashes always flip to forward.
            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath(".claude\\skills");
            Assert.AreEqual(".claude/skills", normalized);
        }

        [Test]
        public void NormalizeSkillsPath_AbsoluteInsideProject_BecomesRelative()
        {
            // The path-portability fix for issue #761: an absolute path inside the project
            // root must be rewritten to a project-relative form with forward slashes.
            var projectRoot = UnityMcpPluginEditor.ProjectRootPath;
            var absInside = Path.Combine(projectRoot, ".claude", "skills");

            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath(absInside);

            Assert.IsFalse(Path.IsPathRooted(normalized),
                $"Expected relative path but got rooted: {normalized}");
            Assert.AreEqual(".claude/skills", normalized);
        }

        [Test]
        public void NormalizeSkillsPath_AbsoluteInsideProject_WithBackslashes_BecomesRelative()
        {
            // Same as above but verify the backslash form is also recognised — this is the
            // exact shape `Path.Combine(...)` produces on Windows.
            var projectRoot = UnityMcpPluginEditor.ProjectRootPath;
            var absInsideBackslash = projectRoot.Replace('/', '\\') + "\\.claude\\skills";

            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath(absInsideBackslash);

            Assert.IsFalse(Path.IsPathRooted(normalized),
                $"Expected relative path but got rooted: {normalized}");
            Assert.AreEqual(".claude/skills", normalized);
        }

        [Test]
        public void NormalizeSkillsPath_AbsoluteOutsideProject_RemainsAbsolute()
        {
            // The user has explicitly chosen an external location — we must not rewrite
            // it. Use a path that is guaranteed to be outside the project root.
            string outsideAbsolute;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Pick a drive root that the project is not on, or a temp path that is
                // definitely not under the project root. `Path.GetTempPath()` is a safe
                // bet because the worktree project root never lives under the temp dir.
                var tempRoot = Path.GetTempPath().TrimEnd('\\', '/');
                outsideAbsolute = Path.Combine(tempRoot, "external-skills");
            }
            else
            {
                outsideAbsolute = "/tmp/external-skills";
            }

            // Sanity check the assumption — if this fires, pick a different fixture path.
            var projectRoot = UnityMcpPluginEditor.ProjectRootPath.Replace('\\', '/').TrimEnd('/') + "/";
            Assume.That(outsideAbsolute.Replace('\\', '/').StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase),
                Is.False, "Test fixture must be outside the project root.");

            var normalized = UnityMcpPluginEditor.NormalizeSkillsPath(outsideAbsolute);

            // The path stayed absolute. Backslashes may have been flipped to forward
            // slashes (for diff stability) — that is intentional and not a portability
            // issue because the path is already a user-chosen external location.
            Assert.IsTrue(Path.IsPathRooted(normalized),
                $"Expected rooted path but got: {normalized}");
            Assert.AreEqual(outsideAbsolute.Replace('\\', '/'), normalized);
        }

        [Test]
        public void NormalizeSkillsPath_NullOrEmpty_ReturnedVerbatim()
        {
            // Defensive: pass-through for empty/null/whitespace. The caller decides what
            // to do; we do not synthesise a default here.
            Assert.AreEqual(string.Empty, UnityMcpPluginEditor.NormalizeSkillsPath(string.Empty));
            Assert.AreEqual("   ", UnityMcpPluginEditor.NormalizeSkillsPath("   "));
        }

        // ----- Setter persistence tests -----------------------------------------------

        [Test]
        public void SkillsPath_Setter_PersistsRelativeForm_WhenGivenAbsoluteInsideProject()
        {
            var projectRoot = UnityMcpPluginEditor.ProjectRootPath;
            var absInside = Path.Combine(projectRoot, ".claude", "skills");

            UnityMcpPluginEditor.SkillsPath = absInside;

            Assert.AreEqual(".claude/skills", UnityMcpPluginEditor.SkillsPath,
                "Setter must normalise absolute-inside-project to forward-slash relative.");
        }

        [Test]
        public void SkillsPath_Setter_PersistsRelativeForm_WhenGivenAlreadyRelative()
        {
            UnityMcpPluginEditor.SkillsPath = ".cursor/skills";

            Assert.AreEqual(".cursor/skills", UnityMcpPluginEditor.SkillsPath);
        }

        [Test]
        public void SkillsPath_Setter_PersistsForwardSlashes_WhenGivenBackslashRelative()
        {
            UnityMcpPluginEditor.SkillsPath = ".cursor\\skills";

            Assert.AreEqual(".cursor/skills", UnityMcpPluginEditor.SkillsPath);
        }

        [Test]
        public void SkillsPath_RoundTripsThroughSave_AsRelative()
        {
            // End-to-end: set an absolute-inside-project path, save, read the on-disk JSON,
            // and confirm the persisted value is the relative form. This is the contract
            // teams need so committing `UserSettings/AI-Game-Developer-Config.json` works.
            var projectRoot = UnityMcpPluginEditor.ProjectRootPath;
            var absInside = Path.Combine(projectRoot, ".claude", "skills");

            UnityMcpPluginEditor.SkillsPath = absInside;
            UnityMcpPluginEditor.Instance.Save();

            var json = File.ReadAllText(UnityMcpPluginEditor.AssetsFileAbsolutePath);

            // Persisted JSON uses camelCase: `"skillsPath": ".claude/skills"`.
            Assert.That(json, Does.Contain("\".claude/skills\""),
                $"Persisted JSON must contain the relative skillsPath. Got:\n{json}");
            Assert.That(json, Does.Not.Contain(projectRoot.Replace("\\", "\\\\")),
                $"Persisted JSON must not contain the machine-specific absolute project root. Got:\n{json}");
        }

        [Test]
        public void SkillsPath_Setter_LeavesExternalAbsoluteAlone()
        {
            string outsideAbsolute;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var tempRoot = Path.GetTempPath().TrimEnd('\\', '/');
                outsideAbsolute = Path.Combine(tempRoot, "external-skills");
            }
            else
            {
                outsideAbsolute = "/tmp/external-skills";
            }

            var projectRoot = UnityMcpPluginEditor.ProjectRootPath.Replace('\\', '/').TrimEnd('/') + "/";
            Assume.That(outsideAbsolute.Replace('\\', '/').StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase),
                Is.False, "Test fixture must be outside the project root.");

            UnityMcpPluginEditor.SkillsPath = outsideAbsolute;

            // The user explicitly chose an external location — the setter MUST NOT rewrite
            // it to a relative path. Forward-slash normalisation is acceptable.
            Assert.AreEqual(outsideAbsolute.Replace('\\', '/'), UnityMcpPluginEditor.SkillsPath);
        }
    }
}
