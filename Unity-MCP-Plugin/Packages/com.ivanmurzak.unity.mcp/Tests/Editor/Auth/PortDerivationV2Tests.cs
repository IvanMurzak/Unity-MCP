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
using com.IvanMurzak.McpPlugin.AgentConfig;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Defect <b>B10</b> fix (auth-fixes d1): <see cref="UnityMcpPlugin.GeneratePortFromDirectory(string)"/>
    /// now derives the deterministic local port through the shared <see cref="ProjectIdentity"/> <b>v2</b>
    /// normalization (trim trailing separators, <c>'\\'</c> → <c>'/'</c>, <see cref="string.ToLowerInvariant"/>)
    /// instead of hashing the raw, untrimmed working-directory string. That keeps the port in lock-step with
    /// the routing pin, so a Windows working directory reported with backslashes maps to the SAME port as its
    /// forward-slash form. Expected values are the committed v2 golden vectors
    /// (<c>MCP-Plugin-dotnet/McpPlugin/src/AgentConfig/ProjectIdentity.GoldenVectors.v2.json</c>).
    /// </summary>
    public class PortDerivationV2Tests
    {
        [Test]
        public void GeneratePortFromDirectory_DelegatesToProjectIdentityV2()
        {
            const string dir = @"C:\Users\user\my-game";
            Assert.AreEqual(ProjectIdentity.DerivePortV2(dir), UnityMcpPlugin.GeneratePortFromDirectory(dir));
        }

        // v2 golden vectors: backslash and forward-slash forms of the same Windows root converge (the B5/B10
        // fix); a trailing separator is trimmed; a POSIX path is unaffected (v2 == v1 there).
        [TestCase(@"C:\Users\user\my-game", 24298)]   // Windows backslash form
        [TestCase(@"C:\Users\user\my-game\", 24298)]  // trailing backslash trimmed → identical
        [TestCase("C:/Users/user/my-game", 24298)]    // forward-slash form → SAME port under v2
        [TestCase("C:/Users/user/my-game/", 24298)]   // trailing slash trimmed → identical
        [TestCase("/home/user/my-game", 23940)]       // POSIX typical → v2 == v1
        [TestCase("/home/user/my-game/", 23940)]      // trailing slash trimmed → identical
        public void GeneratePortFromDirectory_MatchesV2GoldenPort(string dir, int expectedPort)
        {
            Assert.AreEqual(expectedPort, UnityMcpPlugin.GeneratePortFromDirectory(dir));
        }

        [Test]
        public void GeneratePortFromDirectory_BackslashAndForwardSlash_Converge()
        {
            // The defining B10/B5 property: the two separator forms of one root yield the SAME port,
            // whereas the pre-fix raw-string derivation gave two different ports on Windows.
            Assert.AreEqual(
                UnityMcpPlugin.GeneratePortFromDirectory(@"C:\Users\user\my-game"),
                UnityMcpPlugin.GeneratePortFromDirectory("C:/Users/user/my-game"));
        }

        [Test]
        public void GeneratePortFromDirectory_TrailingSeparatorInsensitive()
        {
            Assert.AreEqual(
                UnityMcpPlugin.GeneratePortFromDirectory(@"C:\Users\user\my-game"),
                UnityMcpPlugin.GeneratePortFromDirectory(@"C:\Users\user\my-game\"));
        }

        [Test]
        public void GeneratePortFromDirectory_StaysInDeterministicRange()
        {
            var port = UnityMcpPlugin.GeneratePortFromDirectory(@"C:\Users\user\my-game");
            Assert.GreaterOrEqual(port, ProjectIdentity.MinPort);
            Assert.LessOrEqual(port, ProjectIdentity.MaxPort);
        }

        [Test]
        public void GeneratePortFromDirectory_NullDirectory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => UnityMcpPlugin.GeneratePortFromDirectory(null!));
        }
    }
}
