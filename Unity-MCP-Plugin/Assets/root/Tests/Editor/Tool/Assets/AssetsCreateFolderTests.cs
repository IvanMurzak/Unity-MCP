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
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class AssetsCreateFolderTests : BaseTest
    {
        const string TestFolderName = "Unity-MCP-Test-CreateFolder";

        readonly List<string> _foldersToCleanup = new();

        public override IEnumerator TearDown()
        {
            foreach (var folder in _foldersToCleanup)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    Debug.Log($"Cleaning up test folder: {folder}");
                    AssetDatabase.DeleteAsset(folder);
                }
            }
            _foldersToCleanup.Clear();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return base.TearDown();
        }

        /// <summary>
        /// Calls the assets-create-folder tool and returns the full JSON result string
        /// without asserting success (for testing error responses).
        /// </summary>
        string CallToolAndGetJson(string json)
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            Debug.Log($"{Tool_Assets.AssetsCreateFolderToolId} Started with JSON:\n{json}");

            var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var request = new RequestCallTool(Tool_Assets.AssetsCreateFolderToolId, parameters!);
            var task = McpPlugin.McpPlugin.Instance.McpManager.ToolManager!.RunCallTool(request);
            var result = task.Result;
            var jsonResult = result.ToJson(reflector)!;

            Debug.Log($"{Tool_Assets.AssetsCreateFolderToolId} Result:\n{jsonResult}");

            return jsonResult;
        }

        [Test]
        public void CreateFolder_ValidParentFolder_Succeeds()
        {
            var folderPath = $"Assets/{TestFolderName}";
            _foldersToCleanup.Add(folderPath);

            RunTool(Tool_Assets.AssetsCreateFolderToolId, $@"{{
                ""inputs"": [{{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": ""{TestFolderName}""
                }}]
            }}");

            Assert.IsTrue(AssetDatabase.IsValidFolder(folderPath),
                $"Folder should exist at {folderPath}");
        }

        [Test]
        public void CreateFolder_InvalidParent_NonExistentPath_ReturnsError()
        {
            var jsonResult = CallToolAndGetJson(@"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets/NonExistentFolder12345"",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("Invalid parent folder path", jsonResult);
            StringAssert.Contains("Assets/NonExistentFolder12345", jsonResult);
        }

        [Test]
        public void CreateFolder_InvalidParent_NotStartingWithAssets_ReturnsError()
        {
            var jsonResult = CallToolAndGetJson(@"{
                ""inputs"": [{
                    ""parentFolderPath"": ""SomeRandomPath"",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("Invalid parent folder path", jsonResult);
            StringAssert.Contains("SomeRandomPath", jsonResult);
        }

        [Test]
        public void CreateFolder_InvalidParent_EmptyPath_ReturnsError()
        {
            var jsonResult = CallToolAndGetJson(@"{
                ""inputs"": [{
                    ""parentFolderPath"": """",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("Invalid parent folder path", jsonResult);
        }

        [Test]
        public void CreateFolder_MixedInputs_ValidAndInvalid_PartialSuccess()
        {
            var validFolderPath = $"Assets/{TestFolderName}-Mixed";
            _foldersToCleanup.Add(validFolderPath);

            var jsonResult = CallToolAndGetJson($@"{{
                ""inputs"": [
                    {{
                        ""parentFolderPath"": ""Assets/NonExistentFolder12345"",
                        ""newFolderName"": ""ShouldFail""
                    }},
                    {{
                        ""parentFolderPath"": ""Assets"",
                        ""newFolderName"": ""{TestFolderName}-Mixed""
                    }}
                ]
            }}");

            // Should contain error for the invalid path
            StringAssert.Contains("Invalid parent folder path", jsonResult);
            StringAssert.Contains("Assets/NonExistentFolder12345", jsonResult);

            // Valid folder should still be created
            Assert.IsTrue(AssetDatabase.IsValidFolder(validFolderPath),
                $"Valid folder should still be created at {validFolderPath}");
        }
    }
}
