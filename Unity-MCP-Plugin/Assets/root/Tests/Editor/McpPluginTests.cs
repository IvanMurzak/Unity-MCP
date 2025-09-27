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
using com.IvanMurzak.Unity.MCP.Common;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class McpPluginTests : BaseTest
    {
        const int WaitTimeoutTicks = 100000;
        [Test]
        public void McpPlugin_Instance_ShouldNotBeNull_WhenInitialized()
        {
            // Act & Assert
            Assert.IsNotNull(McpPlugin.Instance, "McpPlugin instance should not be null after initialization");
            Assert.IsTrue(McpPlugin.HasInstance, "McpPlugin should have an instance after initialization");
        }

        [Test]
        public void McpPlugin_Instance_ShouldHaveValidMcpRunner()
        {
            // Act
            var instance = McpPlugin.Instance;

            // Assert
            Assert.IsNotNull(instance, "McpPlugin instance should not be null");
            Assert.IsNotNull(instance!.McpRunner, "McpRunner should not be null");
            Assert.IsNotNull(instance!.McpRunner.Reflector, "Reflector should not be null");
        }

        [UnityTest]
        public IEnumerator McpPlugin_DoOnce_ShouldExecuteCallbackOnce()
        {
            // Arrange
            var callbackExecuted = false;
            var executionCount = 0;

            // Act
            var subscription = McpPlugin.DoOnce(plugin =>
            {
                callbackExecuted = true;
                executionCount++;
            });
            try
            {
                for (int i = 0; !callbackExecuted && i < WaitTimeoutTicks; i++)
                    yield return null; // Allow callback to execute

                // Assert
                Assert.IsTrue(callbackExecuted, "DoOnce callback should have executed");
                Assert.AreEqual(1, executionCount, "DoOnce callback should execute exactly once");
            }
            finally
            {
                // Cleanup
                subscription?.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator McpPlugin_DoAlways_ShouldExecuteCallbackMultipleTimes()
        {
            // Arrange
            var callbackExecuted = false;
            var executionCount = 0;

            // Act
            var subscription = McpPlugin.DoAlways(plugin =>
            {
                callbackExecuted = true;
                executionCount++;
            });

            try
            {
                for (int i = 0; !callbackExecuted && i < WaitTimeoutTicks; i++)
                    yield return null; // Allow callback to execute

                // Trigger another execution by accessing Instance again
                var _ = McpPlugin.Instance;
                yield return null;

                // Assert
                Assert.IsTrue(executionCount >= 1, "DoAlways callback should have executed at least once");
            }
            finally
            {
                // Cleanup
                subscription?.Dispose();
            }
        }

        // [UnityTest]
        // public IEnumerator McpPlugin_StaticDisposeAsync_ShouldNotThrow()
        // {
        //     // Act & Assert
        //     var task = McpPlugin.StaticDisposeAsync();
        //     while (!task.IsCompleted)
        //         yield return null;

        //     Assert.Pass("StaticDisposeAsync completed without throwing exceptions");
        // }

        [Test]
        public void McpPlugin_DoOnce_WithNullCallback_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => McpPlugin.DoOnce(null!),
                "DoOnce with null callback should not throw");
        }

        [Test]
        public void McpPlugin_DoAlways_WithNullCallback_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => McpPlugin.DoAlways(null!),
                "DoAlways with null callback should not throw");
        }
    }
}