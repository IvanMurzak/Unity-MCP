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
using com.IvanMurzak.Unity.MCP.Editor.DevControl;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode unit tests for the PURE-MANAGED <see cref="DevControlRouter"/> — the routing + parsing
    /// core of the DEV-ONLY inject/control bridge. These never touch the live window / HttpListener, so
    /// they run headless in CI (no Unity Editor UI required).
    /// </summary>
    public class DevControlRouterTests
    {
        // ── Route table ─────────────────────────────────────────────────────────────

        [TestCase("GET", "/health", DevControlRouter.Command.Health)]
        [TestCase("GET", "/state", DevControlRouter.Command.State)]
        [TestCase("POST", "/inject/connection-status", DevControlRouter.Command.InjectConnectionStatus)]
        [TestCase("POST", "/inject/server-status", DevControlRouter.Command.InjectServerStatus)]
        [TestCase("POST", "/control/server-url", DevControlRouter.Command.ControlServerUrl)]
        [TestCase("POST", "/control/select-agent", DevControlRouter.Command.ControlSelectAgent)]
        [TestCase("POST", "/control/click", DevControlRouter.Command.ControlClick)]
        public void Route_KnownRoutes_ResolveToCommand(string method, string path, DevControlRouter.Command expected)
        {
            Assert.AreEqual(expected, DevControlRouter.Route(method, path));
        }

        [TestCase("get", "/health", DevControlRouter.Command.Health)]
        [TestCase("PoSt", "/control/click", DevControlRouter.Command.ControlClick)]
        public void Route_MethodIsCaseInsensitive(string method, string path, DevControlRouter.Command expected)
        {
            Assert.AreEqual(expected, DevControlRouter.Route(method, path));
        }

        [TestCase("POST", "/health")]                  // right path, wrong method
        [TestCase("GET", "/inject/connection-status")] // right path, wrong method
        [TestCase("GET", "/HEALTH")]                    // path is case-sensitive
        [TestCase("GET", "/health/")]                   // trailing slash not stripped by the router
        [TestCase("GET", "/unknown")]
        [TestCase("DELETE", "/state")]
        public void Route_UnknownRoutes_ReturnUnknown(string method, string path)
        {
            Assert.AreEqual(DevControlRouter.Command.Unknown, DevControlRouter.Route(method, path));
        }

        [Test]
        public void Route_NullArgs_ReturnUnknown()
        {
            Assert.AreEqual(DevControlRouter.Command.Unknown, DevControlRouter.Route(null, "/health"));
            Assert.AreEqual(DevControlRouter.Command.Unknown, DevControlRouter.Route("GET", null));
            Assert.AreEqual(DevControlRouter.Command.Unknown, DevControlRouter.Route(null, null));
        }

        // ── Connection-status parsing ───────────────────────────────────────────────

        [TestCase("connected", DevControlRouter.ConnectionStatus.Connected)]
        [TestCase("Connected", DevControlRouter.ConnectionStatus.Connected)]
        [TestCase("  CONNECTED  ", DevControlRouter.ConnectionStatus.Connected)]
        [TestCase("connecting", DevControlRouter.ConnectionStatus.Connecting)]
        [TestCase("disconnected", DevControlRouter.ConnectionStatus.Disconnected)]
        public void TryParseConnectionStatus_Valid_ReturnsTrue(string value, DevControlRouter.ConnectionStatus expected)
        {
            Assert.IsTrue(DevControlRouter.TryParseConnectionStatus(value, out var status));
            Assert.AreEqual(expected, status);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("online")]
        [TestCase("running")]
        public void TryParseConnectionStatus_Invalid_ReturnsFalse(string? value)
        {
            Assert.IsFalse(DevControlRouter.TryParseConnectionStatus(value, out var status));
            Assert.AreEqual(DevControlRouter.ConnectionStatus.Disconnected, status); // documented default
        }

        // ── Server-status parsing ───────────────────────────────────────────────────

        [TestCase("stopped", DevControlRouter.ServerStatus.Stopped)]
        [TestCase("Starting", DevControlRouter.ServerStatus.Starting)]
        [TestCase("RUNNING", DevControlRouter.ServerStatus.Running)]
        [TestCase("  stopping ", DevControlRouter.ServerStatus.Stopping)]
        [TestCase("external", DevControlRouter.ServerStatus.External)]
        public void TryParseServerStatus_Valid_ReturnsTrue(string value, DevControlRouter.ServerStatus expected)
        {
            Assert.IsTrue(DevControlRouter.TryParseServerStatus(value, out var status));
            Assert.AreEqual(expected, status);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("connected")]
        [TestCase("paused")]
        public void TryParseServerStatus_Invalid_ReturnsFalse(string? value)
        {
            Assert.IsFalse(DevControlRouter.TryParseServerStatus(value, out var status));
            Assert.AreEqual(DevControlRouter.ServerStatus.Stopped, status); // documented default
        }

        // ── Click-target normalization ──────────────────────────────────────────────

        [TestCase("connect", "connect")]
        [TestCase("Connect", "connect")]
        [TestCase("  DISCONNECT  ", "disconnect")]
        [TestCase("start-server", "start-server")]
        [TestCase("stop-server", "stop-server")]
        [TestCase("authorize", "authorize")]
        [TestCase("generate-token", "generate-token")]
        public void TryNormalizeClickTarget_Valid_ReturnsTrueAndLowercases(string target, string expected)
        {
            Assert.IsTrue(DevControlRouter.TryNormalizeClickTarget(target, out var normalized));
            Assert.AreEqual(expected, normalized);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("configure")]   // a Godot target, not a Unity one
        [TestCase("remove")]
        [TestCase("nonsense")]
        public void TryNormalizeClickTarget_Invalid_ReturnsFalse(string? target)
        {
            Assert.IsFalse(DevControlRouter.TryNormalizeClickTarget(target, out var normalized));
            Assert.AreEqual(string.Empty, normalized);
        }
    }
}
