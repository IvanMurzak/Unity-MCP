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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class TestInfrastructureValidation
    {
        [Test]
        public void TestInfrastructure_BasicAssertion_ShouldPass()
        {
            // Simple test to verify NUnit is working
            Assert.IsTrue(true, "Basic assertion should pass");
            Assert.AreEqual(2, 1 + 1, "Basic math should work");
        }

        [Test]
        public void TestInfrastructure_CanAccessCommonNamespace_ShouldPass()
        {
            // Verify we can access the common namespace types
            var errorUtils = typeof(ErrorUtils);
            Assert.IsNotNull(errorUtils, "Should be able to access ErrorUtils from Common namespace");
        }

        [Test]
        public void TestInfrastructure_MockClassesWork_ShouldPass()
        {
            // Verify mock classes can be instantiated
            var mockLogger = new MockLogger<TestInfrastructureValidation>();
            Assert.IsNotNull(mockLogger, "Mock logger should be instantiable");
            Assert.IsFalse(mockLogger.HasErrorLogs, "Mock logger should start with no error logs");
        }
    }
}