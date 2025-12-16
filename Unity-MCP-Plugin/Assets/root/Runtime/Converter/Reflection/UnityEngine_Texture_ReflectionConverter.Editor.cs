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
#if UNITY_EDITOR
using System;
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Texture_ReflectionConverter : UnityEngine_Asset_ReflectionConverter<UnityEngine.Texture>
    {
        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var baseResult = base.TryDeserializeValueInternal(
                reflector: reflector,
                data: data,
                result: out result,
                type: type,
                depth: depth,
                logs: logs,
                logger: logger);

            if (result is UnityEngine.Texture)
                return baseResult;

            if (result is UnityEngine.Sprite sprite)
            {
                var path = AssetDatabase.GetAssetPath(sprite);
                result = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<UnityEngine.Texture>()
                    .FirstOrDefault();
                return result != null;
            }
            return baseResult;
        }

        protected override UnityEngine.Texture? LoadFromInstanceID(int instanceID)
        {
            var textureOrSprite = EditorUtility.InstanceIDToObject(instanceID);
            if (textureOrSprite == null) return null;

            if (textureOrSprite is UnityEngine.Texture texture)
                return texture;

            if (textureOrSprite is UnityEngine.Sprite sprite)
            {
                var path = AssetDatabase.GetAssetPath(sprite);
                return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<UnityEngine.Texture>()
                    .FirstOrDefault();
            }
            return null;
        }

        protected override UnityEngine.Texture? LoadFromAssetPath(string path)
        {
            var allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            return allAssets
               .OfType<UnityEngine.Texture>()
               .FirstOrDefault();
        }
    }
}
#endif
