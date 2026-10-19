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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class CliSessionLeaseTests
    {
        string _projectRoot = null!;
        string _leasePath = null!;

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(Path.GetTempPath(), "cli-session-lease-" + Guid.NewGuid().ToString("N"));
            _leasePath = CliSessionLease.PathFor(_projectRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(_leasePath)!);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_projectRoot))
                Directory.Delete(_projectRoot, recursive: true);
        }

        void WriteLease(string json) => File.WriteAllText(_leasePath, json);

        [Test]
        public void PathFor_IsTheCliCoreLeaseLocation()
        {
            Assert.AreEqual(Path.Combine(_projectRoot, ".ai-game-dev", "session.json"), _leasePath);
        }

        [Test]
        public void TryDeleteOwn_RemovesALeaseWithTheSameNonce()
        {
            WriteLease("{\"pid\":42,\"nonce\":\"abc\",\"engine\":\"unity\",\"mode\":\"headless\",\"ownedBy\":\"cli\"}");
            Assert.IsTrue(CliSessionLease.TryDeleteOwn(_leasePath, "abc"));
            Assert.IsFalse(File.Exists(_leasePath));
        }

        [Test]
        public void TryDeleteOwn_KeepsAnotherSessionsLease()
        {
            WriteLease("{\"pid\":42,\"nonce\":\"other\"}");
            Assert.IsFalse(CliSessionLease.TryDeleteOwn(_leasePath, "abc"));
            Assert.IsTrue(File.Exists(_leasePath));
        }

        [TestCase("not json")]
        [TestCase("[]")]
        [TestCase("{\"nonce\":7}")]
        [TestCase("{}")]
        public void TryDeleteOwn_KeepsAnUnreadableLease(string json)
        {
            WriteLease(json);
            Assert.IsFalse(CliSessionLease.TryDeleteOwn(_leasePath, "abc"));
            Assert.IsTrue(File.Exists(_leasePath));
        }

        [Test]
        public void TryDeleteOwn_NoLeaseIsNotAnError()
        {
            Assert.IsFalse(CliSessionLease.TryDeleteOwn(_leasePath, "abc"));
        }
    }
}
