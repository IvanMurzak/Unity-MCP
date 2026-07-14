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
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// mcp-authorize PR 3 — verifies the shared <see cref="ProjectIdentity"/> derivation, as compiled into the
    /// vendored McpPlugin 7.0 DLL and run under Unity's Mono/IL2CPP runtime, reproduces the committed
    /// cross-language golden vectors byte-for-byte. The canonical source of these values is
    /// <c>MCP-Plugin-dotnet/McpPlugin/src/AgentConfig/ProjectIdentity.GoldenVectors.json</c> (the routing pin +
    /// deterministic local port a Unity project reports in its instance-metadata handshake).
    /// <para>
    /// The one committed vector NOT covered here is the U+0130 (LATIN CAPITAL LETTER I WITH DOT ABOVE) Unicode
    /// case (<c>/home/İstanbul/game</c>). That vector exists specifically to pin the C# <c>ToLowerInvariant</c>
    /// vs JS <c>toLowerCase()</c> divergence for the TS CLI port; asserting it under Unity's Mono runtime
    /// (whose invariant-culture case tables can differ from .NET Core's) would test the host runtime's
    /// globalization rather than this wiring. Every ASCII/Latin path below is runtime-stable.
    /// </para>
    /// </summary>
    public class ProjectIdentityGoldenVectorTests
    {
        // Golden vectors (path, pin, port) copied verbatim from ProjectIdentity.GoldenVectors.json.
        // A backslash in the C# literal ("\\") is a single backslash in the path string.
        [TestCase("/home/user/my-game", "34ea75f2", 23940)]     // POSIX typical project path
        [TestCase("/home/user/my-game/", "34ea75f2", 23940)]    // trailing slash trimmed -> identical
        [TestCase("/home/USER/My-Game", "34ea75f2", 23940)]     // case-folded -> identical
        [TestCase("C:\\Users\\user\\my-game", "8ef72cf7", 29310)]   // Windows backslash form
        [TestCase("C:\\Users\\user\\my-game\\", "8ef72cf7", 29310)] // trailing backslash trimmed -> identical
        [TestCase("C:/Users/user/my-game", "5a87324e", 24298)]  // forward-slash form DIFFERS (separators not normalized)
        [TestCase("/srv/games/space sim", "08c6cbb6", 27816)]   // path containing a space
        public void ProjectIdentity_ReproducesGoldenVector(string path, string expectedPin, int expectedPort)
        {
            Assert.AreEqual(expectedPin, ProjectIdentity.DerivePin(path));
            Assert.AreEqual(expectedPort, ProjectIdentity.DerivePort(path));

            var id = ProjectIdentity.Derive(path);
            Assert.AreEqual(expectedPin, id.Pin);
            Assert.AreEqual(expectedPort, id.Port);
            Assert.IsFalse(id.PortIsOverridden);

            // The full project-path hash (sent in the handshake) begins with the routing pin, so the server
            // pin-matches by prefix.
            var hash = ProjectIdentity.DeriveProjectPathHash(path);
            Assert.AreEqual(64, hash.Length);
            StringAssert.StartsWith(expectedPin, hash);
        }

        [TestCase("34ea75f2", 23940)]
        [TestCase("8ef72cf7", 29310)]
        [TestCase("5a87324e", 24298)]
        [TestCase("08c6cbb6", 27816)]
        public void EveryGoldenVector_HasValidPinAndPortShape(string expectedPin, int expectedPort)
        {
            Assert.AreEqual(ProjectIdentity.PinLength, expectedPin.Length);
            foreach (var c in expectedPin)
                Assert.IsTrue((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'), $"pin '{expectedPin}' must be lowercase hex");

            Assert.GreaterOrEqual(expectedPort, ProjectIdentity.MinPort);
            Assert.LessOrEqual(expectedPort, ProjectIdentity.MaxPort);
        }

        [Test]
        public void Separators_AreNotNormalized_ForwardAndBackslashDiffer()
        {
            Assert.AreNotEqual(
                ProjectIdentity.DerivePort("C:/Users/user/my-game"),
                ProjectIdentity.DerivePort("C:\\Users\\user\\my-game"));
        }

        [Test]
        public void TrailingSeparator_IsTrimmed_SameIdentityAsWithout()
        {
            Assert.AreEqual(
                ProjectIdentity.DerivePin("/home/user/my-game"),
                ProjectIdentity.DerivePin("/home/user/my-game/"));
            Assert.AreEqual(
                ProjectIdentity.DerivePort("C:\\Games\\proj"),
                ProjectIdentity.DerivePort("C:\\Games\\proj\\"));
        }

        [Test]
        public void CaseFolding_IsApplied()
        {
            Assert.AreEqual(
                ProjectIdentity.DerivePin("/home/user/my-game"),
                ProjectIdentity.DerivePin("/HOME/User/My-Game"));
        }

        [Test]
        public void PortOverride_AlwaysWins_PinUnchanged()
        {
            const string path = "/home/user/my-game";
            var overridden = ProjectIdentity.Derive(path, portOverride: 12345);

            Assert.AreEqual(12345, overridden.Port);
            Assert.IsTrue(overridden.PortIsOverridden);
            Assert.AreEqual(ProjectIdentity.DerivePin(path), overridden.Pin);
        }
    }
}
