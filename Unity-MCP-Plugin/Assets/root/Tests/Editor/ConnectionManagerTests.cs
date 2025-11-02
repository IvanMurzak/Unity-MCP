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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ConnectionManagerTests
    {
        private ILogger<ConnectionManager> _mockLogger = null!;
        private MockHubEndpointConnectionProvider _mockConnectionProvider = null!;
        private ConnectionManager? _connectionManager;
        private McpPlugin.Common.Version _testVersion = new();

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new MockLogger<ConnectionManager>();
            _mockConnectionProvider = new MockHubEndpointConnectionProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _connectionManager?.Dispose();
        }

        [Test]
        public void ConnectionManager_Constructor_ShouldInitializeCorrectly()
        {
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: string.Empty,
                hubConnectionBuilder: _mockConnectionProvider);

            // Assert
            Assert.IsNotNull(_connectionManager, "ConnectionManager should be initialized");
            Assert.AreEqual(HubConnectionState.Disconnected, _connectionManager.ConnectionState.CurrentValue,
                "Initial connection state should be Disconnected");
            Assert.IsFalse(_connectionManager.KeepConnected.CurrentValue,
                "Initial keep connected state should be false");
        }

        [Test]
        public void ConnectionManager_Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            ;

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                _connectionManager = new ConnectionManager(
                    logger: null!,
                    apiVersion: _testVersion,
                    endpoint: string.Empty,
                    hubConnectionBuilder: _mockConnectionProvider),
                "Constructor should throw ArgumentNullException for null logger");
        }

        [Test]
        public void ConnectionManager_Constructor_WithNullBuilder_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                _connectionManager = new ConnectionManager(
                    logger: _mockLogger,
                    apiVersion: _testVersion,
                    endpoint: string.Empty,
                    hubConnectionBuilder: null!),
                "Constructor should throw ArgumentNullException for null builder");
        }

        [Test]
        public void ConnectionManager_Endpoint_ShouldBeSettable()
        {
            // Arrange
            const string testEndpoint = "http://localhost:8080/hub";

            // Act
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: testEndpoint,
                hubConnectionBuilder: _mockConnectionProvider);

            // Assert
            Assert.AreEqual(testEndpoint, _connectionManager.Endpoint,
                "Endpoint property should be settable and gettable");
        }

        [UnityTest]
        public IEnumerator ConnectionManager_Connect_WithInvalidEndpoint_ShouldReturnFalse()
        {
            // Arrange
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: "invalid-endpoint",
                hubConnectionBuilder: _mockConnectionProvider);

            _mockConnectionProvider.ShouldFailConnection = true;

            // Act
            var task = _connectionManager.Connect(CancellationToken.None);
            while (!task.IsCompleted) yield return null;
            var result = task.Result;

            // Assert
            Assert.IsFalse(result, "Connect should return false for invalid endpoint");
        }

        [Test]
        public void ConnectionManager_Endpoint_ShouldHandleValidUrls()
        {
            // Arrange & Act
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: "http://localhost:8080/hub",
                hubConnectionBuilder: _mockConnectionProvider);

            // Assert
            Assert.AreEqual("http://localhost:8080/hub", _connectionManager.Endpoint,
                "Endpoint should accept valid URLs");
        }

        [UnityTest]
        public IEnumerator ConnectionManager_Disconnect_ShouldSetKeepConnectedToFalse()
        {
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: string.Empty,
                hubConnectionBuilder: _mockConnectionProvider);

            // Act
            var task = _connectionManager.Disconnect(CancellationToken.None);
            while (!task.IsCompleted) yield return null;

            // Assert
            Assert.IsFalse(_connectionManager.KeepConnected.CurrentValue,
                "Keep connected should be false after disconnect");
        }

        // [UnityTest]
        // public IEnumerator ConnectionManager_InvokeAsync_WithoutConnection_ShouldLogError()
        // {
        //     // Arrange
        //     var mockLogger = new MockLogger<ConnectionManager>();
        //     var manager = new ConnectionManager(mockLogger, _mockBuilder);

        //     // Act
        //     var task = manager.InvokeAsync(methodName: "TestMethod", input: "TestInput");
        //     while (!task.IsCompleted) yield return null;

        //     // Assert
        //     Assert.IsTrue(mockLogger.HasErrorLogs, "Should log error when invoking without connection");
        // }

        [Test]
        public void ConnectionManager_Dispose_ShouldNotThrow()
        {
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: string.Empty,
                hubConnectionBuilder: _mockConnectionProvider);

            // Act & Assert
            Assert.DoesNotThrow(() => _connectionManager.Dispose(),
                "Dispose should not throw exceptions");
        }

        [Test]
        public void ConnectionManager_DisposeAsync_ShouldNotThrow()
        {
            _connectionManager = new ConnectionManager(
                logger: _mockLogger,
                apiVersion: _testVersion,
                endpoint: string.Empty,
                hubConnectionBuilder: _mockConnectionProvider);

            // Act & Assert
            Assert.DoesNotThrow(() => _connectionManager.DisposeAsync().GetAwaiter().GetResult(),
                "DisposeAsync should not throw exceptions");
        }
    }

    // Mock classes for testing
    public class MockLogger<T> : ILogger<T>
    {
        public bool HasErrorLogs { get; private set; }
        public bool HasWarningLogs { get; private set; }

        public System.IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Debug.Log($"[{logLevel}] {message}");

            if (logLevel == LogLevel.Error) HasErrorLogs = true;
            if (logLevel == LogLevel.Warning) HasWarningLogs = true;
        }
    }

    public class MockHubEndpointConnectionProvider : IHubConnectionProvider
    {
        public bool ShouldFailConnection { get; set; } = false;

        public async Task<HubConnection> CreateConnectionAsync(string endpoint)
        {
            await Task.Delay(10); // Simulate async operation

            if (ShouldFailConnection)
                return null!;

            // For testing purposes, we return null to avoid complex mocking
            // Real implementation would create a HubConnection
            return null!;
        }
    }
}