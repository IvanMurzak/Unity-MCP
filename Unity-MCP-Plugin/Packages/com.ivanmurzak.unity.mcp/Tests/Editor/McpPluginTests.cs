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
using NUnit.Framework;
using R3;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class McpPluginTests : BaseTest
    {
        const int WaitTimeoutTicks = 100000;

        [Test]
        public void CurrentPlugin_ShouldNotBeNull_WhenInitialized()
        {
            // Act & Assert
            Assert.IsNotNull(UnityMcpPluginEditor.Instance.McpPluginInstance, "CurrentPlugin should not be null after initialization");
        }

        [Test]
        public void CurrentPlugin_ShouldHaveValidMcpManager()
        {
            // Act
            var plugin = UnityMcpPluginEditor.Instance.McpPluginInstance;

            // Assert
            Assert.IsNotNull(plugin, "CurrentPlugin should not be null");
            Assert.IsNotNull(plugin!.McpManager, "McpManager should not be null");
            Assert.IsNotNull(plugin!.McpManager.Reflector, "Reflector should not be null");
        }

        [UnityTest]
        public IEnumerator PluginProperty_WhereNotNull_Take1_ShouldExecuteCallbackOnce()
        {
            // Arrange
            var callbackExecuted = false;
            var executionCount = 0;

            // Act
            var subscription = UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Take(1)
                .Subscribe(plugin =>
                {
                    callbackExecuted = true;
                    executionCount++;
                });

            try
            {
                for (int i = 0; !callbackExecuted && i < WaitTimeoutTicks; i++)
                    yield return null;

                // Assert
                Assert.IsTrue(callbackExecuted, "PluginProperty callback should have executed");
                Assert.AreEqual(1, executionCount, "Take(1) callback should execute exactly once");
            }
            finally
            {
                subscription?.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator PluginProperty_WhereNotNull_ShouldExecuteCallbackAtLeastOnce()
        {
            // Arrange
            var callbackExecuted = false;
            var executionCount = 0;

            // Act
            var subscription = UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
                {
                    callbackExecuted = true;
                    executionCount++;
                });

            try
            {
                for (int i = 0; !callbackExecuted && i < WaitTimeoutTicks; i++)
                    yield return null;

                // Assert
                Assert.IsTrue(executionCount >= 1, "PluginProperty callback should have executed at least once");
            }
            finally
            {
                subscription?.Dispose();
            }
        }
    }
}
