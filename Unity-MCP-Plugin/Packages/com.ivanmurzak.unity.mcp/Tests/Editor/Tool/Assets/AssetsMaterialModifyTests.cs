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
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class AssetsMaterialModifyTests : BaseTest
    {
        [Test]
        public void Material_Modify_Color()
        {
            var assetPath = "Assets/Materials/Glass.mat";

            var folderEx = new CreateFolderExecutor("Assets", "Materials");

            folderEx
                .AddChild(() =>
                {
                    var material = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(material, assetPath);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                })
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Assets).GetMethod(nameof(Tool_Assets.Modify)),
                    json: JsonTestUtils.Fill(@"{
                        ""assetRef"": {
                            ""instanceID"": 0,
                            ""assetPath"": ""{assetPath}""
                        },
                        ""content"": {
                            ""name"": ""Glass"",
                            ""typeName"": ""UnityEngine.Material"",
                            ""props"": [
                                {
                                    ""name"": ""_Color"",
                                    ""typeName"": ""UnityEngine.Color"",
                                    ""value"": {
                                        ""r"": 1.0,
                                        ""g"": 0.4,
                                        ""b"": 0.7,
                                        ""a"": 0.35
                                    }
                                }
                            ]
                        }
                    }",
                    new Dictionary<string, object?>
                    {
                        { "{assetPath}", assetPath }
                    }))
                )
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    Assert.IsNotNull(material, $"Material should exist at path: {assetPath}");

                    var color = material.GetColor("_Color");
                    Assert.AreEqual(1.0f, color.r, 0.01f, "Color.r should be 1.0");
                    Assert.AreEqual(0.4f, color.g, 0.01f, "Color.g should be 0.4");
                    Assert.AreEqual(0.7f, color.b, 0.01f, "Color.b should be 0.7");
                    Assert.AreEqual(0.35f, color.a, 0.01f, "Color.a should be 0.35");
                })
                .Execute();
        }
    }
}
