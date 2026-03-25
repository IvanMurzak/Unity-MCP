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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class AssetsShaderTests : BaseTest
    {
        const string ValidShaderSource = @"
Shader ""Test/ValidShader""
{
    Properties
    {
        _Color (""Main Color"", Color) = (1,1,1,1)
        _MainTex (""Base (RGB)"", 2D) = ""white"" {}
        _Glossiness (""Smoothness"", Range(0,1)) = 0.5
        _Metallic (""Metallic"", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack ""Diffuse""
}";

        const string BrokenShaderSource = @"
Shader ""Test/BrokenShader""
{
    Properties
    {
        _Color (""Main Color"", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = undeclaredVariable;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack ""Diffuse""
}";

        [Test]
        public void Shader_GetData_ValidShader()
        {
            var shaderEx = new CreateShaderExecutor(
                shaderFileName: "TestValidShader__.shader",
                shaderSource: ValidShaderSource,
                "Assets", "Unity-MCP-Test", "Shaders"
            );

            shaderEx
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Assets_Shader).GetMethod(nameof(Tool_Assets_Shader.GetData)),
                    json: JsonTestUtils.Fill(@"{
                        ""assetRef"": {
                            ""assetPath"": ""{assetPath}""
                        },
                        ""includeProperties"": true
                    }",
                    new Dictionary<string, object?>
                    {
                        { "{assetPath}", shaderEx.AssetPath }
                    }))
                )
                .AddChild(new ValidateToolResultExecutor())
                .AddChild<ResponseData<ResponseCallTool>>(result =>
                {
                    var reflector = UnityMcpPluginEditor.Instance.Reflector;
                    var jsonResult = result.ToJson(reflector)!;
                    Assert.IsTrue(jsonResult.Contains("HasErrors"), "Response should contain 'HasErrors' field.");
                    Assert.IsTrue(jsonResult.Contains("false"), "Response should contain 'false' value (HasErrors) for valid shader.");
                    Assert.IsTrue(jsonResult.Contains("_Color"), "Response should contain '_Color' property from valid shader.");
                    Assert.IsTrue(jsonResult.Contains("_MainTex"), "Response should contain '_MainTex' property from valid shader.");
                    Assert.IsTrue(jsonResult.Contains("Opaque"), "Response should contain 'Opaque' RenderType from valid shader.");
                })
                .AddChild(() =>
                {
                    var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(shaderEx.AssetPath);
                    Assert.IsNotNull(shader, $"Shader should exist at path: {shaderEx.AssetPath}");
                    Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "Valid shader should have no errors.");
                })
                .Execute();
        }

        [Test]
        public void Shader_GetData_BrokenShader()
        {
            var shaderEx = new CreateShaderExecutor(
                shaderFileName: "TestBrokenShader__.shader",
                shaderSource: BrokenShaderSource,
                "Assets", "Unity-MCP-Test", "Shaders"
            );

            // Expect the shader compilation error log that Unity emits during import
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Shader error in 'Test/BrokenShader'"));

            shaderEx
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Assets_Shader).GetMethod(nameof(Tool_Assets_Shader.GetData)),
                    json: JsonTestUtils.Fill(@"{
                        ""assetRef"": {
                            ""assetPath"": ""{assetPath}""
                        }
                    }",
                    new Dictionary<string, object?>
                    {
                        { "{assetPath}", shaderEx.AssetPath }
                    }))
                )
                .AddChild(new ValidateToolResultExecutor())
                .AddChild<ResponseData<ResponseCallTool>>(result =>
                {
                    var reflector = UnityMcpPluginEditor.Instance.Reflector;
                    var jsonResult = result.ToJson(reflector)!;
                    Assert.IsTrue(jsonResult.Contains("HasErrors"), "Response should contain 'HasErrors' field.");
                    Assert.IsTrue(jsonResult.Contains("true"), "Response should contain 'true' value (HasErrors) for broken shader.");
                    Assert.IsTrue(jsonResult.Contains("Messages"), "Response should contain 'Messages' field for broken shader.");
                    Assert.IsTrue(jsonResult.Contains("Severity"), "Response should contain 'Severity' field in shader messages.");
                    Assert.IsTrue(jsonResult.Contains("Line"), "Response should contain 'Line' field in shader error messages.");
                })
                .AddChild(() =>
                {
                    var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(shaderEx.AssetPath);
                    Assert.IsNotNull(shader, $"Shader should exist at path: {shaderEx.AssetPath}");
                    Assert.IsTrue(ShaderUtil.ShaderHasError(shader), "Broken shader should have errors.");

                    var messages = ShaderUtil.GetShaderMessages(shader);
                    Assert.IsNotNull(messages, "Broken shader should have error messages.");
                    Assert.Greater(messages.Length, 0, "Broken shader should have at least one error message.");
                })
                .Execute();
        }
    }
}
