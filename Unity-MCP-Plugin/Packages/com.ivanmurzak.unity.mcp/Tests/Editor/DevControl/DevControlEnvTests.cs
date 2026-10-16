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
using com.IvanMurzak.Unity.MCP.Editor.DevControl;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode unit tests for the PURE-MANAGED <see cref="DevControlEnv"/> — the .env-file layer of the
    /// process-env &gt; .env &gt; default precedence chain. Process-env precedence is exercised against a
    /// uniquely-named var so the test never collides with a real export. File parsing is exercised
    /// against a temp .env written per-test.
    /// </summary>
    public class DevControlEnvTests
    {
        string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "unity-mcp-devctrl-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
            catch { /* best effort */ }
        }

        void WriteEnv(string contents) => File.WriteAllText(Path.Combine(_tempDir, ".env"), contents);

        // ── LookupRaw (file parsing) ────────────────────────────────────────────────

        [Test]
        public void LookupRaw_ReadsKey_SkipsCommentsAndBlanks()
        {
            WriteEnv("# a comment\n\nUNITY_MCP_DEV_CONTROL=1\nOTHER=x\n");
            Assert.AreEqual("1", DevControlEnv.LookupRaw(Path.Combine(_tempDir, ".env"), "UNITY_MCP_DEV_CONTROL"));
        }

        [Test]
        public void LookupRaw_StripsWrappingQuotes_AndWhitespace()
        {
            WriteEnv("UNITY_MCP_DEV_CONTROL_PORT = \"9933\" \n");
            Assert.AreEqual("9933", DevControlEnv.LookupRaw(Path.Combine(_tempDir, ".env"), "UNITY_MCP_DEV_CONTROL_PORT"));
        }

        [Test]
        public void LookupRaw_LastOccurrenceWins()
        {
            WriteEnv("K=first\nK=second\n");
            Assert.AreEqual("second", DevControlEnv.LookupRaw(Path.Combine(_tempDir, ".env"), "K"));
        }

        [Test]
        public void LookupRaw_MissingFileOrKeyOrBlank_ReturnsNull()
        {
            Assert.IsNull(DevControlEnv.LookupRaw(Path.Combine(_tempDir, "nope.env"), "K"));
            WriteEnv("K=\nOTHER=y\n");
            Assert.IsNull(DevControlEnv.LookupRaw(Path.Combine(_tempDir, ".env"), "K"));      // blank value
            Assert.IsNull(DevControlEnv.LookupRaw(Path.Combine(_tempDir, ".env"), "ABSENT")); // absent key
        }

        // ── Resolve / IsEnabled / ResolvePort ───────────────────────────────────────

        [Test]
        public void Resolve_FileLayer_UsedWhenProcessEnvUnset()
        {
            WriteEnv("UNITY_MCP_DEV_CONTROL=1\n");
            Assert.AreEqual("1", DevControlEnv.Resolve("UNITY_MCP_DEV_CONTROL", _tempDir));
            Assert.IsTrue(DevControlEnv.IsEnabled(_tempDir));
        }

        [Test]
        public void IsEnabled_FalseWhenFlagMissingOrNotOne()
        {
            Assert.IsFalse(DevControlEnv.IsEnabled(_tempDir)); // no .env at all
            WriteEnv("UNITY_MCP_DEV_CONTROL=0\n");
            Assert.IsFalse(DevControlEnv.IsEnabled(_tempDir));
            WriteEnv("UNITY_MCP_DEV_CONTROL=true\n");
            Assert.IsFalse(DevControlEnv.IsEnabled(_tempDir)); // only exactly "1" enables
        }

        [Test]
        public void ResolvePort_FallsBackToDefault_OnUnsetOrInvalid()
        {
            Assert.AreEqual(9922, DevControlEnv.ResolvePort(_tempDir, 9922)); // unset
            WriteEnv("UNITY_MCP_DEV_CONTROL_PORT=not-a-number\n");
            Assert.AreEqual(9922, DevControlEnv.ResolvePort(_tempDir, 9922)); // unparseable
            WriteEnv("UNITY_MCP_DEV_CONTROL_PORT=70000\n");
            Assert.AreEqual(9922, DevControlEnv.ResolvePort(_tempDir, 9922)); // out of range
            WriteEnv("UNITY_MCP_DEV_CONTROL_PORT=9933\n");
            Assert.AreEqual(9933, DevControlEnv.ResolvePort(_tempDir, 9922)); // valid override
        }

        [Test]
        public void Resolve_ProcessEnv_OutranksFile()
        {
            const string key = "UNITY_MCP_DEVCTRL_TEST_PRECEDENCE";
            WriteEnv($"{key}=from-file\n");
            try
            {
                Environment.SetEnvironmentVariable(key, "from-process");
                Assert.AreEqual("from-process", DevControlEnv.Resolve(key, _tempDir));
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, null);
            }
            // With the process env cleared, the file layer is used.
            Assert.AreEqual("from-file", DevControlEnv.Resolve(key, _tempDir));
        }
    }
}
