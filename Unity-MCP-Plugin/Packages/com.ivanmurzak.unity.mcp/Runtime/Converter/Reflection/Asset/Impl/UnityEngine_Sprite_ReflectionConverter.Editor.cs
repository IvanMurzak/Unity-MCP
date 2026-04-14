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
#if UNITY_EDITOR && UNITY_6000_5_OR_NEWER
using System;
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Sprite_ReflectionConverter : UnityEngine_Asset_ReflectionConverter<UnityEngine.Sprite>
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

            if (result is UnityEngine.Sprite)
                return baseResult;

            if (result is UnityEngine.Texture2D texture)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
                result = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<UnityEngine.Sprite>()
                    .FirstOrDefault();
                return result != null;
            }
            return baseResult;
        }

        protected override UnityEngine.Sprite? LoadFromEntityId(UnityEngine.EntityId entityId)
        {
            var textureOrSprite = UnityEditor.EditorUtility.EntityIdToObject(entityId);
            if (textureOrSprite == null) return null;

            if (textureOrSprite is UnityEngine.Sprite sprite)
                return sprite;

            if (textureOrSprite is UnityEngine.Texture2D texture)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
                return UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<UnityEngine.Sprite>()
                    .FirstOrDefault();
            }
            return null;
        }

        protected override UnityEngine.Sprite? LoadFromAssetPath(string path)
        {
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            return allAssets
               .OfType<UnityEngine.Sprite>()
               .FirstOrDefault();
        }
    }
}
#endif
