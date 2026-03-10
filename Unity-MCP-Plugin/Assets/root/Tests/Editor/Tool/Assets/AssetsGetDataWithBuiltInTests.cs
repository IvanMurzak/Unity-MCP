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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class AssetsGetDataWithBuiltInTests : BaseTest
    {
        [Test]
        public void GetData_BuiltInMaterial_ReturnsData()
        {
            var tool = new Tool_Assets();
            var assetPath = $"{ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath}/Default-Material.mat";
            var assetRef = new AssetObjectRef { AssetPath = assetPath };

            var result = tool.GetData(assetRef);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("Default-Material", result.name);
            Assert.AreEqual("UnityEngine.Material", result.typeName);
        }

        [Test]
        public void GetData_BuiltInAsset_NotFound_ThrowsException()
        {
            var tool = new Tool_Assets();
            var assetPath = $"{ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath}/NonExistentAsset.mat";
            var assetRef = new AssetObjectRef { AssetPath = assetPath };

            var ex = Assert.Throws<System.Exception>(() =>
            {
                tool.GetData(assetRef);
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("not found"), "Exception message should indicate asset was not found");
        }

        [Test]
        public void GetData_BuiltInShader_ReturnsData()
        {
            var tool = new Tool_Assets();
            // "Standard" shader is a common built-in shader
            var assetPath = $"{ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath}/Standard.shader";
            var assetRef = new AssetObjectRef { AssetPath = assetPath };

            var result = tool.GetData(assetRef);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("Standard", result.name);
            Assert.AreEqual("UnityEngine.Shader", result.typeName);
        }
    }
}
