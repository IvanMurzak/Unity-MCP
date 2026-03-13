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
using com.IvanMurzak.Unity.MCP.Editor.Services;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;
using TransportMethod = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.TransportMethod;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class MainWindowEditorStatusLogicTests
    {
        #region GetConnectionStatusClass

        [TestCase(HubConnectionState.Connected, true, MainWindowEditor.USS_Connected)]
        [TestCase(HubConnectionState.Connected, false, MainWindowEditor.USS_Disconnected)]
        [TestCase(HubConnectionState.Disconnected, true, MainWindowEditor.USS_Connecting)]
        [TestCase(HubConnectionState.Disconnected, false, MainWindowEditor.USS_Disconnected)]
        [TestCase(HubConnectionState.Reconnecting, true, MainWindowEditor.USS_Connecting)]
        [TestCase(HubConnectionState.Reconnecting, false, MainWindowEditor.USS_Disconnected)]
        public void GetConnectionStatusClass(HubConnectionState state, bool keepConnected, string expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.GetConnectionStatusClass(state, keepConnected));
        }

        #endregion

        #region GetConnectionStatusText

        [TestCase(HubConnectionState.Connected, true, "Connected")]
        [TestCase(HubConnectionState.Connected, false, "Disconnected")]
        [TestCase(HubConnectionState.Disconnected, true, "Connecting...")]
        [TestCase(HubConnectionState.Disconnected, false, "Disconnected")]
        [TestCase(HubConnectionState.Reconnecting, true, "Connecting...")]
        [TestCase(HubConnectionState.Reconnecting, false, "Disconnected")]
        public void GetConnectionStatusText(HubConnectionState state, bool keepConnected, string expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.GetConnectionStatusText(state, keepConnected));
        }

        #endregion

        #region GetButtonText

        [TestCase(HubConnectionState.Connected, true, "Disconnect")]
        [TestCase(HubConnectionState.Connected, false, "Connect")]
        [TestCase(HubConnectionState.Disconnected, true, "Stop")]
        [TestCase(HubConnectionState.Disconnected, false, "Connect")]
        [TestCase(HubConnectionState.Reconnecting, true, "Stop")]
        [TestCase(HubConnectionState.Reconnecting, false, "Connect")]
        public void GetButtonText(HubConnectionState state, bool keepConnected, string expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.GetButtonText(state, keepConnected));
        }

        #endregion

        #region GetServerButtonText

        [TestCase(McpServerStatus.Running, "Stop")]
        [TestCase(McpServerStatus.Starting, "Starting...")]
        [TestCase(McpServerStatus.Stopping, "Stopping...")]
        [TestCase(McpServerStatus.Stopped, "Start")]
        [TestCase(McpServerStatus.External, "External")]
        public void GetServerButtonText(McpServerStatus status, string expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.GetServerButtonText(status));
        }

        #endregion

        #region GetServerStatusClass

        [TestCase(McpServerStatus.Running, MainWindowEditor.USS_Connected)]
        [TestCase(McpServerStatus.Starting, MainWindowEditor.USS_Connecting)]
        [TestCase(McpServerStatus.Stopping, MainWindowEditor.USS_Connecting)]
        [TestCase(McpServerStatus.Stopped, MainWindowEditor.USS_Disconnected)]
        [TestCase(McpServerStatus.External, MainWindowEditor.USS_External)]
        public void GetServerStatusClass(McpServerStatus status, string expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.GetServerStatusClass(status));
        }

        #endregion

        #region IsServerButtonEnabled

        [TestCase(McpServerStatus.Running, true)]
        [TestCase(McpServerStatus.Starting, false)]
        [TestCase(McpServerStatus.Stopping, false)]
        [TestCase(McpServerStatus.Stopped, true)]
        [TestCase(McpServerStatus.External, false)]
        public void IsServerButtonEnabled(McpServerStatus status, bool expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.IsServerButtonEnabled(status));
        }

        #endregion

        #region CombineMcpServerStatus

        [TestCase(McpServerStatus.Stopped, true, McpServerStatus.External)]
        [TestCase(McpServerStatus.Starting, true, McpServerStatus.External)]
        [TestCase(McpServerStatus.Running, true, McpServerStatus.Running)]
        [TestCase(McpServerStatus.Stopped, false, McpServerStatus.Stopped)]
        [TestCase(McpServerStatus.Running, false, McpServerStatus.Running)]
        [TestCase(McpServerStatus.Stopping, true, McpServerStatus.External)]
        [TestCase(McpServerStatus.Stopping, false, McpServerStatus.Stopping)]
        public void CombineMcpServerStatus(McpServerStatus status, bool isConnected, McpServerStatus expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.CombineMcpServerStatus(status, isConnected));
        }

        #endregion

        #region GetServerLabelText

        [Test]
        public void GetServerLabelText_Running()
        {
            Assert.AreEqual("MCP server: Running (http)", MainWindowEditor.GetServerLabelText(McpServerStatus.Running, null));
        }

        [Test]
        public void GetServerLabelText_Starting()
        {
            Assert.AreEqual("MCP server: Starting... (http)", MainWindowEditor.GetServerLabelText(McpServerStatus.Starting, null));
        }

        [Test]
        public void GetServerLabelText_Stopping()
        {
            Assert.AreEqual("MCP server: Stopping... (http)", MainWindowEditor.GetServerLabelText(McpServerStatus.Stopping, null));
        }

        [Test]
        public void GetServerLabelText_Stopped()
        {
            Assert.AreEqual("MCP server", MainWindowEditor.GetServerLabelText(McpServerStatus.Stopped, null));
        }

        [Test]
        public void GetServerLabelText_External_NullTransport()
        {
            Assert.AreEqual("MCP server: External", MainWindowEditor.GetServerLabelText(McpServerStatus.External, null));
        }

        [Test]
        public void GetServerLabelText_External_Stdio()
        {
            Assert.AreEqual("MCP server: External (stdio)", MainWindowEditor.GetServerLabelText(McpServerStatus.External, TransportMethod.stdio));
        }

        [Test]
        public void GetServerLabelText_External_Http()
        {
            Assert.AreEqual("MCP server: External (http)", MainWindowEditor.GetServerLabelText(McpServerStatus.External, TransportMethod.streamableHttp));
        }

        #endregion

        #region ComputeCloudAuthState

        [Test]
        public void ComputeCloudAuthState_Cloud_NullToken()
        {
            var (needsAuth, hasToken, isCloud) = MainWindowEditor.ComputeCloudAuthState(ConnectionMode.Cloud, null);
            Assert.IsTrue(needsAuth);
            Assert.IsFalse(hasToken);
            Assert.IsTrue(isCloud);
        }

        [Test]
        public void ComputeCloudAuthState_Cloud_EmptyToken()
        {
            var (needsAuth, hasToken, isCloud) = MainWindowEditor.ComputeCloudAuthState(ConnectionMode.Cloud, "");
            Assert.IsTrue(needsAuth);
            Assert.IsFalse(hasToken);
            Assert.IsTrue(isCloud);
        }

        [Test]
        public void ComputeCloudAuthState_Cloud_ValidToken()
        {
            var (needsAuth, hasToken, isCloud) = MainWindowEditor.ComputeCloudAuthState(ConnectionMode.Cloud, "abc");
            Assert.IsFalse(needsAuth);
            Assert.IsTrue(hasToken);
            Assert.IsTrue(isCloud);
        }

        [Test]
        public void ComputeCloudAuthState_Custom_NullToken()
        {
            var (needsAuth, hasToken, isCloud) = MainWindowEditor.ComputeCloudAuthState(ConnectionMode.Custom, null);
            Assert.IsFalse(needsAuth);
            Assert.IsFalse(hasToken);
            Assert.IsFalse(isCloud);
        }

        [Test]
        public void ComputeCloudAuthState_Custom_ValidToken()
        {
            var (needsAuth, hasToken, isCloud) = MainWindowEditor.ComputeCloudAuthState(ConnectionMode.Custom, "abc");
            Assert.IsFalse(needsAuth);
            Assert.IsTrue(hasToken);
            Assert.IsFalse(isCloud);
        }

        #endregion

        #region IsAuthFlowRunning

        [TestCase(DeviceAuthFlowState.Initiating, true)]
        [TestCase(DeviceAuthFlowState.WaitingForUser, true)]
        [TestCase(DeviceAuthFlowState.Polling, true)]
        [TestCase(DeviceAuthFlowState.Idle, false)]
        [TestCase(DeviceAuthFlowState.Authorized, false)]
        [TestCase(DeviceAuthFlowState.Failed, false)]
        [TestCase(DeviceAuthFlowState.Expired, false)]
        [TestCase(DeviceAuthFlowState.Cancelled, false)]
        public void IsAuthFlowRunning(DeviceAuthFlowState state, bool expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.IsAuthFlowRunning(state));
        }

        #endregion

        #region GetAuthFlowStatusMessage

        [Test]
        public void GetAuthFlowStatusMessage_Initiating()
        {
            Assert.AreEqual("Initiating...", MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Initiating, null, null));
        }

        [Test]
        public void GetAuthFlowStatusMessage_WaitingForUser()
        {
            var result = MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.WaitingForUser, "ABC123", null);
            Assert.AreEqual("Code: ABC123 — Authorize in browser", result);
        }

        [Test]
        public void GetAuthFlowStatusMessage_Polling()
        {
            var result = MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Polling, "XYZ789", null);
            Assert.AreEqual("Code: XYZ789 — Waiting for authorization...", result);
        }

        [Test]
        public void GetAuthFlowStatusMessage_Authorized()
        {
            Assert.AreEqual("Authorized!", MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Authorized, null, null));
        }

        [Test]
        public void GetAuthFlowStatusMessage_Failed_WithMessage()
        {
            var result = MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Failed, null, "timeout");
            Assert.AreEqual("Failed: timeout", result);
        }

        [Test]
        public void GetAuthFlowStatusMessage_Failed_NullMessage()
        {
            var result = MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Failed, null, null);
            Assert.AreEqual("Failed: ", result);
        }

        [Test]
        public void GetAuthFlowStatusMessage_Expired()
        {
            Assert.AreEqual("Expired — try again", MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Expired, null, null));
        }

        [Test]
        public void GetAuthFlowStatusMessage_Cancelled()
        {
            Assert.AreEqual("Cancelled", MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Cancelled, null, null));
        }

        [Test]
        public void GetAuthFlowStatusMessage_Idle()
        {
            Assert.AreEqual("", MainWindowEditor.GetAuthFlowStatusMessage(DeviceAuthFlowState.Idle, null, null));
        }

        #endregion

        #region IsHostFieldReadOnly

        [TestCase(true, HubConnectionState.Disconnected, true)]
        [TestCase(true, HubConnectionState.Connected, true)]
        [TestCase(true, HubConnectionState.Reconnecting, true)]
        [TestCase(false, HubConnectionState.Disconnected, false)]
        [TestCase(false, HubConnectionState.Connected, true)]
        [TestCase(false, HubConnectionState.Reconnecting, true)]
        public void IsHostFieldReadOnly(bool keepConnected, HubConnectionState state, bool expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.IsHostFieldReadOnly(keepConnected, state));
        }

        #endregion

        #region IsMcpServerControlEnabled

        [TestCase(TransportMethod.stdio, false)]
        [TestCase(TransportMethod.streamableHttp, true)]
        public void IsMcpServerControlEnabled(TransportMethod transport, bool expected)
        {
            Assert.AreEqual(expected, MainWindowEditor.IsMcpServerControlEnabled(transport));
        }

        #endregion
    }
}
