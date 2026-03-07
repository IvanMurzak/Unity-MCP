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
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestsRunTests
    {
        [Test]
        public void TestsRun_ClassFilter_Pattern_MatchesAssetsCreateFolderTests()
        {
            const string className = "AssetsCreateFolderTests";
            var pattern = Tool_Tests.CreateClassRegexPattern(className);

            // Full test names from AssetsCreateFolderTests that should match
            Assert.IsTrue(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests.CreateFolder_ValidParentFolder_Succeeds",
                pattern),
                "Should match CreateFolder_ValidParentFolder_Succeeds");

            Assert.IsTrue(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests.CreateFolder_InvalidParent_NonExistentPath_ReturnsError",
                pattern),
                "Should match CreateFolder_InvalidParent_NonExistentPath_ReturnsError");

            Assert.IsTrue(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests.CreateFolder_EmptyFolderName_ReturnsError",
                pattern),
                "Should match CreateFolder_EmptyFolderName_ReturnsError");

            Assert.IsTrue(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests.CreateFolder_FolderNameWithForwardSlash_ReturnsError",
                pattern),
                "Should match CreateFolder_FolderNameWithForwardSlash_ReturnsError");
        }

        [Test]
        public void TestsRun_ClassFilter_Pattern_DoesNotMatchOtherClasses()
        {
            const string className = "AssetsCreateFolderTests";
            var pattern = Tool_Tests.CreateClassRegexPattern(className);

            // Tests from a different class should not match
            Assert.IsFalse(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsFindBuiltInTests.SomeTest",
                pattern),
                "Should not match a different test class");

            // Class name alone (no method) should not match
            Assert.IsFalse(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests",
                pattern),
                "Should not match class name without a method");

            // A class with a longer name that contains the class name should not match
            Assert.IsFalse(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.SubAssetsCreateFolderTests.SomeTest",
                pattern),
                "Should not match a class whose name contains the target as a substring");
        }

        [Test]
        public void TestsRun_NamespaceFilter_Pattern_MatchesExpectedNamespace()
        {
            const string namespaceName = "com.IvanMurzak.Unity.MCP.Editor.Tests";
            var pattern = Tool_Tests.CreateNamespaceRegexPattern(namespaceName);

            // Test names in the namespace should match
            Assert.IsTrue(Regex.IsMatch(
                "com.IvanMurzak.Unity.MCP.Editor.Tests.AssetsCreateFolderTests.CreateFolder_ValidParentFolder_Succeeds",
                pattern),
                "Should match test in the target namespace");

            // Test names in a different namespace should not match
            Assert.IsFalse(Regex.IsMatch(
                "some.other.namespace.SomeTests.SomeMethod",
                pattern),
                "Should not match test in a different namespace");
        }

        [Test]
        public void TestsRun_FilterParams_ForAssetsCreateFolderTests_HasCorrectState()
        {
            const string className = "AssetsCreateFolderTests";
            var filterParams = new TestFilterParameters(testClass: className);

            Assert.IsTrue(filterParams.HasAnyFilter, "Filter should report HasAnyFilter = true");
            Assert.AreEqual(className, filterParams.TestClass, "TestClass should be set correctly");
            Assert.IsNull(filterParams.TestAssembly, "TestAssembly should be null");
            Assert.IsNull(filterParams.TestNamespace, "TestNamespace should be null");
            Assert.IsNull(filterParams.TestMethod, "TestMethod should be null");
        }

        [Test]
        public void TestsRun_FilterParams_ToString_ContainsClassName()
        {
            const string className = "AssetsCreateFolderTests";
            var filterParams = new TestFilterParameters(testClass: className);

            var description = filterParams.ToString();
            StringAssert.Contains(className, description,
                "Filter description should contain the class name");
        }
    }
}
