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
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ScriptReadTests : BaseTest
    {
        bool? _originalEnabled;

        // Use this very file's source as a known existing .cs file for read tests
        const string TestScriptPath = "Assets/root/Tests/Editor/Tool/Script/ScriptReadTests.cs";

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalEnabled = toolManager!.IsToolEnabled(Tool_Script.ScriptReadToolId);
            toolManager.SetToolEnabled(Tool_Script.ScriptReadToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Script.ScriptReadToolId, _originalEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        static string GetTestScriptAbsolutePath()
        {
            // Convert Unity relative path to absolute path
            var unityProjectPath = Directory.GetCurrentDirectory();
            return Path.Combine(unityProjectPath, TestScriptPath.Replace('/', Path.DirectorySeparatorChar));
        }

        [UnityTest]
        public IEnumerator ScriptRead_BaseTestFile_FromMainThread_ReturnsContent()
        {
            yield return null;

            // Use BaseTest.cs since it definitely exists
            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            var result = Tool_Script.Read(baseTestPath);
            Assert.IsNotNull(result, "Read() should return content");
            Assert.IsNotEmpty(result, "Read() should return non-empty content");
            StringAssert.Contains("BaseTest", result, "Content should contain 'BaseTest'");
        }

        [UnityTest]
        public IEnumerator ScriptRead_BaseTestFile_FromBackgroundThread_ReturnsContent()
        {
            yield return null;

            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            yield return RunOnBackgroundThread(() =>
            {
                var result = Tool_Script.Read(baseTestPath);
                Assert.IsNotNull(result, "Read() should return content from background thread");
                Assert.IsNotEmpty(result, "Read() should return non-empty content from background thread");
            });
        }

        [UnityTest]
        public IEnumerator ScriptRead_ViaRunTool_FromMainThread_Succeeds()
        {
            yield return null;

            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            RunTool(Tool_Script.ScriptReadToolId, $@"{{
                ""filePath"": ""{baseTestPath}""
            }}");
        }

        [UnityTest]
        public IEnumerator ScriptRead_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Script.ScriptReadToolId, $@"{{
                    ""filePath"": ""{baseTestPath}""
                }}"));
        }

        [UnityTest]
        public IEnumerator ScriptRead_WithLineRange_ReturnsPartialContent()
        {
            yield return null;

            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            var fullContent = Tool_Script.Read(baseTestPath);
            var partialContent = Tool_Script.Read(baseTestPath, lineFrom: 1, lineTo: 5);

            Assert.IsNotEmpty(partialContent, "Partial read should return content");
            Assert.LessOrEqual(partialContent.Length, fullContent.Length,
                "Partial read should return less than or equal to full content");
        }

        [UnityTest]
        public IEnumerator ScriptRead_WithLineRange_FromBackgroundThread_ReturnsContent()
        {
            yield return null;

            const string baseTestPath = "Assets/root/Tests/Editor/BaseTest.cs";
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(),
                baseTestPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                Assert.Ignore($"Test script not found at {absolutePath}, skipping test.");
                yield break;
            }

            yield return RunOnBackgroundThread(() =>
            {
                var result = Tool_Script.Read(baseTestPath, lineFrom: 1, lineTo: 10);
                Assert.IsNotEmpty(result, "Partial read from background thread should return content");
            });
        }

        [UnityTest]
        public IEnumerator ScriptRead_EmptyPath_ThrowsArgumentException()
        {
            yield return null;

            Assert.Throws<ArgumentException>(() => Tool_Script.Read(string.Empty));
        }

        [UnityTest]
        public IEnumerator ScriptRead_NonCsFile_ThrowsArgumentException()
        {
            yield return null;

            Assert.Throws<ArgumentException>(() => Tool_Script.Read("Assets/SomeFile.txt"));
        }

        [UnityTest]
        public IEnumerator ScriptRead_NonExistentFile_ThrowsFileNotFoundException()
        {
            yield return null;

            Assert.Throws<FileNotFoundException>(() =>
                Tool_Script.Read("Assets/NonExistentScript12345.cs"));
        }

        [UnityTest]
        public IEnumerator ScriptRead_EmptyPath_FromBackgroundThread_ThrowsArgumentException()
        {
            yield return null;

            Exception? caughtException = null;
            yield return RunOnBackgroundThread(() =>
            {
                try
                {
                    Tool_Script.Read(string.Empty);
                }
                catch (ArgumentException ex)
                {
                    caughtException = ex;
                }
            });

            Assert.IsNotNull(caughtException, "Should throw ArgumentException from background thread too");
        }
    }
}
