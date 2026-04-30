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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
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
            "or '" + AssetsShaderListAllToolId + "' tool to list all shader names.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements via Reflector.TryReadAt, or '" + "viewQuery" + "' (a ViewQuery) to navigate to a " +
            "subtree and/or filter by name regex / max depth / type via Reflector.View. The result populates " +
            "'View' on the returned ShaderData. These two parameters are mutually exclusive.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public AIGD.ShaderData GetData
        (
            AssetObjectRef assetRef,
            [Description("Include compilation error and warning messages. Default: true")]
            bool? includeMessages = true,
            [Description("Include shader properties (uniforms) list. Default: false")]
            bool? includeProperties = false,
            [Description("Include subshader and pass structure. Default: false")]
            bool? includeSubshaders = false,
            [Description("Include pass source code in subshader data. Requires 'includeSubshaders' to be true. Can produce very large responses. Default: false")]
            bool? includeSourceCode = false,
            [Description("Optional. List of paths to read individually via Reflector.TryReadAt against the underlying Shader asset. " +
                "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. " +
                "Mutually exclusive with '" + "viewQuery" + "'.")]
            List<string>? paths = null,
            [Description("Optional. View-query filter routed through Reflector.View against the underlying Shader asset. " +
                "Mutually exclusive with '" + "paths" + "'.")]
            ViewQuery? viewQuery = null
        )
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(assetRef));

            var hasPaths = paths != null && paths.Count > 0;
            var hasViewQuery = viewQuery != null;
            if (hasPaths && hasViewQuery)
                throw new ArgumentException(
                    $"'{nameof(paths)}' and '{nameof(viewQuery)}' are mutually exclusive — supply at most one.");

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

                var data = BuildShaderData(shader, options);

                if (hasPaths || hasViewQuery)
                {
                    var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                    var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets_Shader>();

                    if (hasPaths)
                        data.View = PathReadHelper.BuildPathReadAggregate(reflector, shader, shader.name, paths!, logger);
                    else
                        data.View = reflector.View(shader, viewQuery, logs: null, logger: logger)
                            ?? new SerializedMember { name = shader.name, typeName = shader.GetType().FullName ?? string.Empty };
                }

                return data;
            });
        }

        static AIGD.ShaderData BuildShaderData(Shader shader, ShaderDataOptions options)
        {
            var data = new AIGD.ShaderData
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
                    data.Messages = messages.Select(msg => new AIGD.ShaderMessageData
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
                    data.Properties = new List<AIGD.ShaderPropertyData>(propertyCount);
                    for (var i = 0; i < propertyCount; i++)
                    {
                        var propType = shader.GetPropertyType(i);
                        var prop = new AIGD.ShaderPropertyData
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
                        data.Subshaders = new List<AIGD.SubshaderData>(subshaderCount);
                        for (var s = 0; s < subshaderCount; s++)
                        {
                            var subshader = shaderData.GetSubshader(s);
                            var subshaderData = new AIGD.SubshaderData
                            {
                                Index = s,
                                PassCount = subshader.PassCount
                            };

                            if (subshader.PassCount > 0)
                            {
                                subshaderData.Passes = new List<AIGD.PassData>(subshader.PassCount);
                                for (var p = 0; p < subshader.PassCount; p++)
                                {
                                    var pass = subshader.GetPass(p);
                                    subshaderData.Passes.Add(new AIGD.PassData
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

    }
}
