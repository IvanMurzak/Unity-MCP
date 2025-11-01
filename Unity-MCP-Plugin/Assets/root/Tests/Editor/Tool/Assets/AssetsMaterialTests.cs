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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class AssetsMaterialTests : BaseTest
    {
        [Test]
        public void Material_Create()
        {
            var materialEx = new CreateMaterialExecutor(
                materialName: "TestMaterial__.mat",
                shaderName: "Standard",
                "Assets", "Unity-MCP-Test", "Materials"
            );

            materialEx
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Assets).GetMethod(nameof(Tool_Assets.CreateMaterial)),
                    json: JsonTestUtils.Fill(@"{
                        ""assetPath"": ""{assetPath}"",
                        ""shaderName"": ""Standard""
                    }",
                    new Dictionary<string, object?>
                    {
                        { "{assetPath}", materialEx.AssetPath }
                    }))
                )
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialEx.AssetPath);
                    Assert.IsNotNull(material, $"Material should be created at path: {materialEx.AssetPath}");
                    Assert.AreEqual("Standard", material.shader.name, "Material shader should be 'Standard'.");
                })
                .Execute();
        }

        // [Test]
        // public void Material_Modify()
        // {
        //     var reflector = McpPlugin.Instance!.McpRunner.Reflector;

        //     var propertyName = "_Metallic";
        //     var propertyValue = 1;

        //     var materialEx = new CreateMaterialExecutor(
        //         materialName: "TestMaterial.mat",
        //         shaderName: "Standard",
        //         "Assets", "Unity-MCP-Test", "Materials"
        //     );

        //     materialEx
        //         .AddChild(new CallToolExecutor(
        //             toolMethod: typeof(Tool_Assets).GetMethod(nameof(Tool_Assets.Modify)),
        //             json: JsonTestUtils.Fill(@"{
        //                 ""assetRef"": {
        //                     ""{assetPathProperty}"": ""{assetPath}""
        //                 },
        //                 ""content"":
        //                 {
        //                     ""typeName"": ""UnityEngine.Material"",
        //                     ""value"": {
        //                         ""{propertyName}"": {propertyValue}
        //                     }
        //                 }
        //             }",
        //             new Dictionary<string, object?>
        //             {
        //                 { "{assetPathProperty}", AssetObjectRef.AssetObjectRefProperty.AssetPath },
        //                 { "{assetPath}", materialEx.AssetPath },
        //                 { "{propertyName}", propertyName },
        //                 { "{propertyValue}", propertyValue }
        //             }))
        //         )
        //         .AddChild(new ValidateToolResultExecutor())
        //         .AddChild(() =>
        //         {
        //             var actualValue = materialEx.Asset?.GetFloat(propertyName);
        //             Assert.AreEqual(propertyValue, actualValue,
        //                 $"Material property '{propertyName}' should be set to {propertyValue}.");
        //         })
        //         .Execute();
        // }
    }
}
