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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Shader
    {
        public const string AssetsShaderGetDataToolId = "assets-shader-get-data";

        [McpPluginTool
        (
            AssetsShaderGetDataToolId,
            Title = "Assets / Shader / Get Data",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("Get detailed data about a shader asset in the Unity project. " +
            "Returns shader properties, subshaders, passes, compilation errors, and supported status. " +
            "Use '" + Tool_Assets.AssetsFindToolId + "' tool with filter 't:Shader' to find shaders, " +
            "or '" + AssetsShaderListAllToolId + "' tool to list all shader names.")]
        public ShaderData GetData
        (
            AssetObjectRef assetRef,
            [Description("Include compilation error and warning messages. Default: true")]
            bool? includeMessages = true,
            [Description("Include shader properties (uniforms) list. Default: false")]
            bool? includeProperties = false,
            [Description("Include subshader and pass structure. Default: false")]
            bool? includeSubshaders = false,
            [Description("Include pass source code in subshader data. Requires 'includeSubshaders' to be true. Can produce very large responses. Default: false")]
            bool? includeSourceCode = false
        )
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(assetRef));

            var resolvedIncludeSourceCode = includeSourceCode ?? false;
            var options = new ShaderDataOptions
            {
                IncludeMessages = includeMessages ?? false,
                IncludeProperties = includeProperties ?? false,
                IncludeSubshaders = (includeSubshaders ?? false) || resolvedIncludeSourceCode,
                IncludeSourceCode = resolvedIncludeSourceCode
            };

            return MainThread.Instance.Run(() =>
            {
                var asset = assetRef.FindAssetObject();
                if (asset == null)
                    throw new Exception(Tool_Assets.Error.NotFoundAsset(assetRef.AssetPath ?? "N/A", assetRef.AssetGuid ?? "N/A"));

                var shader = asset as Shader;
                if (shader == null)
                    throw new ArgumentException($"Asset at '{assetRef.AssetPath}' is not a Shader. It is a '{asset.GetType().Name}'.", nameof(assetRef));

                return BuildShaderData(shader, options);
            });
        }

        static ShaderData BuildShaderData(Shader shader, ShaderDataOptions options)
        {
            var data = new ShaderData
            {
                Reference = new AssetObjectRef(shader),
                Name = shader.name,
                IsSupported = shader.isSupported,
                RenderQueue = shader.renderQueue,
                HasErrors = ShaderUtil.ShaderHasError(shader),
                PropertyCount = shader.GetPropertyCount(),
                PassCount = shader.passCount
            };

            if (options.IncludeMessages)
            {
                var messages = ShaderUtil.GetShaderMessages(shader);
                if (messages != null && messages.Length > 0)
                {
                    data.Messages = messages.Select(msg => new ShaderMessageData
                    {
                        Message = msg.message,
                        Line = msg.line,
                        Severity = msg.severity.ToString(),
                        Platform = msg.platform.ToString()
                    }).ToList();
                }
            }

            if (options.IncludeProperties)
            {
                var propertyCount = data.PropertyCount;
                if (propertyCount > 0)
                {
                    data.Properties = new List<ShaderPropertyData>(propertyCount);
                    for (var i = 0; i < propertyCount; i++)
                    {
                        var propType = shader.GetPropertyType(i);
                        var prop = new ShaderPropertyData
                        {
                            Name = shader.GetPropertyName(i),
                            Description = shader.GetPropertyDescription(i),
                            Type = propType.ToString(),
                            Flags = shader.GetPropertyFlags(i).ToString(),
                            NameId = shader.GetPropertyNameId(i)
                        };
                        if (propType == ShaderPropertyType.Range)
                        {
                            var rangeLimits = shader.GetPropertyRangeLimits(i);
                            prop.RangeMin = rangeLimits.x;
                            prop.RangeMax = rangeLimits.y;
                        }

                        if (propType == ShaderPropertyType.Texture)
                        {
                            var defaultTextureName = shader.GetPropertyTextureDefaultName(i);
                            if (!string.IsNullOrEmpty(defaultTextureName))
                                prop.DefaultTextureName = defaultTextureName;
                        }

                        var attributes = shader.GetPropertyAttributes(i);
                        if (attributes != null && attributes.Length > 0)
                            prop.Attributes = attributes.ToList();

                        data.Properties.Add(prop);
                    }
                }
            }

            if (options.IncludeSubshaders)
            {
                var shaderData = ShaderUtil.GetShaderData(shader);
                if (shaderData != null)
                {
                    var subshaderCount = shaderData.SubshaderCount;
                    if (subshaderCount > 0)
                    {
                        data.Subshaders = new List<SubshaderData>(subshaderCount);
                        for (var s = 0; s < subshaderCount; s++)
                        {
                            var subshader = shaderData.GetSubshader(s);
                            var subshaderData = new SubshaderData
                            {
                                Index = s,
                                PassCount = subshader.PassCount
                            };

                            if (subshader.PassCount > 0)
                            {
                                subshaderData.Passes = new List<PassData>(subshader.PassCount);
                                for (var p = 0; p < subshader.PassCount; p++)
                                {
                                    var pass = subshader.GetPass(p);
                                    subshaderData.Passes.Add(new PassData
                                    {
                                        Index = p,
                                        Name = string.IsNullOrEmpty(pass.Name) ? null : pass.Name,
                                        SourceCode = options.IncludeSourceCode ? pass.SourceCode : null
                                    });
                                }
                            }

                            data.Subshaders.Add(subshaderData);
                        }
                    }
                }
            }

            if (shader.passCount > 0)
            {
                var renderType = shader.FindPassTagValue(0, new ShaderTagId("RenderType")).name;
                data.RenderType = string.IsNullOrEmpty(renderType) ? null : renderType;
            }

            return data;
        }

        struct ShaderDataOptions
        {
            public bool IncludeMessages;
            public bool IncludeProperties;
            public bool IncludeSubshaders;
            public bool IncludeSourceCode;
        }

        public class ShaderData
        {
            [Description("Reference to the shader asset for future operations.")]
            public AssetObjectRef? Reference { get; set; }

            [Description("Full name of the shader (e.g. 'Standard', 'Universal Render Pipeline/Lit').")]
            public string? Name { get; set; }

            [Description("Whether the shader is supported on the current GPU and platform.")]
            public bool IsSupported { get; set; }

            [Description("The render queue value of the shader.")]
            public int RenderQueue { get; set; }

            [Description("Whether the shader has any compilation errors.")]
            public bool HasErrors { get; set; }

            [Description("Number of properties exposed by the shader.")]
            public int PropertyCount { get; set; }

            [Description("Total number of passes in the shader.")]
            public int PassCount { get; set; }

            [Description("The RenderType tag value from the first pass, if set.")]
            public string? RenderType { get; set; }

            [Description("Compilation messages including errors and warnings. Null if no messages.")]
            public List<ShaderMessageData>? Messages { get; set; }

            [Description("List of shader properties (uniforms). Null if the shader has no properties.")]
            public List<ShaderPropertyData>? Properties { get; set; }

            [Description("List of subshaders with their passes. Null if shader data is unavailable.")]
            public List<SubshaderData>? Subshaders { get; set; }
        }

        public class ShaderMessageData
        {
            [Description("The error or warning message text.")]
            public string? Message { get; set; }

            [Description("The line number in the shader source where the issue occurs.")]
            public int Line { get; set; }

            [Description("Severity level (e.g. 'Error', 'Warning').")]
            public string? Severity { get; set; }

            [Description("The platform on which the error occurs (e.g. 'OpenGLCore', 'D3D11').")]
            public string? Platform { get; set; }
        }

        public class ShaderPropertyData
        {
            [Description("Property name as used in shader code (e.g. '_MainTex', '_Color').")]
            public string? Name { get; set; }

            [Description("Human-readable description/display name of the property.")]
            public string? Description { get; set; }

            [Description("Property type (e.g. 'Color', 'Float', 'Range', 'Texture', 'Vector', 'Int').")]
            public string? Type { get; set; }

            [Description("Property flags (e.g. 'None', 'HideInInspector', 'PerRendererData').")]
            public string? Flags { get; set; }

            [Description("The unique name ID for this property.")]
            public int NameId { get; set; }

            [Description("Minimum value for Range properties. Null for non-range properties.")]
            public float? RangeMin { get; set; }

            [Description("Maximum value for Range properties. Null for non-range properties.")]
            public float? RangeMax { get; set; }

            [Description("Default texture name for Texture properties. Null if not applicable.")]
            public string? DefaultTextureName { get; set; }

            [Description("Custom attributes applied to this property. Null if none.")]
            public List<string>? Attributes { get; set; }
        }

        public class SubshaderData
        {
            [Description("Index of this subshader within the shader.")]
            public int Index { get; set; }

            [Description("Number of passes in this subshader.")]
            public int PassCount { get; set; }

            [Description("List of passes in this subshader. Null if no passes.")]
            public List<PassData>? Passes { get; set; }
        }

        public class PassData
        {
            [Description("Index of this pass within the subshader.")]
            public int Index { get; set; }

            [Description("Name of the pass. Null if unnamed.")]
            public string? Name { get; set; }

            [Description("Source code of the pass. Null if unavailable.")]
            public string? SourceCode { get; set; }
        }
    }
}
