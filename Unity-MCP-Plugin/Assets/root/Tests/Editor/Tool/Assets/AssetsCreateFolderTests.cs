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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class AssetsCreateFolderTests : BaseTest
    {
        const string TestFolderName = "Unity-MCP-Test-CreateFolder";

        [UnityTest]
        public IEnumerator CreateFolder_ValidParentFolder_Succeeds()
        {
            new CreateFolderExecutor("Assets", TestFolderName)
                .AddChild(new CallToolExecutor(
                    Tool_Assets.AssetsCreateFolderToolId,
                    $@"{{
                        ""inputs"": [{{
                            ""parentFolderPath"": ""Assets/{TestFolderName}"",
                            ""newFolderName"": ""ValidChild""
                        }}]
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsTrue(AssetDatabase.IsValidFolder($"Assets/{TestFolderName}/ValidChild"),
                        $"Folder should exist at Assets/{TestFolderName}/ValidChild");
                })
                .Execute();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_InvalidParent_NonExistentPath_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets/NonExistentFolder12345"",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("invalid parent folder path", jsonResult);
            StringAssert.Contains("Assets/NonExistentFolder12345", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_InvalidParent_NotStartingWithAssets_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""SomeRandomPath"",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("invalid parent folder path", jsonResult);
            StringAssert.Contains("SomeRandomPath", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_InvalidParent_EmptyPath_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": """",
                    ""newFolderName"": ""TestFolder""
                }]
            }");

            StringAssert.Contains("invalid parent folder path", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_MixedInputs_ValidAndInvalid_PartialSuccess()
        {
            new CreateFolderExecutor("Assets", TestFolderName)
                .AddChild(() =>
                {
                    var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, $@"{{
                        ""inputs"": [
                            {{
                                ""parentFolderPath"": ""Assets/NonExistentFolder12345"",
                                ""newFolderName"": ""ShouldFail""
                            }},
                            {{
                                ""parentFolderPath"": ""Assets/{TestFolderName}"",
                                ""newFolderName"": ""Mixed""
                            }}
                        ]
                    }}");

                    // Should contain error for the invalid path
                    StringAssert.Contains("invalid parent folder path", jsonResult);
                    StringAssert.Contains("Assets/NonExistentFolder12345", jsonResult);

                    // Valid folder should still be created
                    Assert.IsTrue(AssetDatabase.IsValidFolder($"Assets/{TestFolderName}/Mixed"),
                        $"Valid folder should still be created at Assets/{TestFolderName}/Mixed");
                })
                .Execute();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_DuplicateName_ReturnsError()
        {
            var dupFolderPath = $"Assets/{TestFolderName}/Dup";

            new CreateFolderExecutor("Assets", TestFolderName)
                .AddChild(new CallToolExecutor(
                    Tool_Assets.AssetsCreateFolderToolId,
                    $@"{{
                        ""inputs"": [{{
                            ""parentFolderPath"": ""Assets/{TestFolderName}"",
                            ""newFolderName"": ""Dup""
                        }}]
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsTrue(AssetDatabase.IsValidFolder(dupFolderPath),
                        $"Folder should exist at {dupFolderPath}");

                    var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, $@"{{
                        ""inputs"": [{{
                            ""parentFolderPath"": ""Assets/{TestFolderName}"",
                            ""newFolderName"": ""Dup""
                        }}]
                    }}");

                    StringAssert.Contains("a folder with the same name already exists", jsonResult);
                    StringAssert.Contains(dupFolderPath, jsonResult);

                    Assert.IsTrue(AssetDatabase.IsValidFolder(dupFolderPath),
                        $"Original folder should still exist at {dupFolderPath}");
                })
                .Execute();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_EmptyFolderName_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": """"
                }]
            }");

            StringAssert.Contains("folder name is empty or whitespace", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_WhitespaceFolderName_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": ""   ""
                }]
            }");

            StringAssert.Contains("folder name is empty or whitespace", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_NullFolderName_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": null
                }]
            }");

            StringAssert.Contains("folder name is empty or whitespace", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_FolderNameWithForwardSlash_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": ""Invalid/Name""
                }]
            }");

            StringAssert.Contains("contains invalid character", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_FolderNameWithBackslash_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": ""Invalid\\Name""
                }]
            }");

            StringAssert.Contains("contains invalid character", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateFolder_FolderNameWithSpecialCharacters_ReturnsError()
        {
            var jsonResult = RunToolRaw(Tool_Assets.AssetsCreateFolderToolId, @"{
                ""inputs"": [{
                    ""parentFolderPath"": ""Assets"",
                    ""newFolderName"": ""Invalid<Name>""
                }]
            }");

            StringAssert.Contains("contains invalid character", jsonResult);
            yield return null;
        }
    }
}
