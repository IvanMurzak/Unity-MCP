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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_Sprite_ReflectionConvertor : UnityEngine_Asset_ReflectionConvertor<UnityEngine.Sprite>
    {
        protected override UnityEngine.Sprite? LoadFromInstanceID(int instanceID)
        {
            var textureOrSprite = EditorUtility.InstanceIDToObject(instanceID);
            if (textureOrSprite == null) return null;

            if (textureOrSprite is Texture2D texture)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<Sprite>()
                    .ToArray();
                return sprites.FirstOrDefault();
            }
            if (textureOrSprite is Sprite sprite)
            {
                return sprite;
            }
            return null;
        }

        protected override UnityEngine.Sprite? LoadFromAssetPath(string path)
        {
            var allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            var sprites = allAssets
               .OfType<Sprite>()
               .ToArray();
            return sprites.FirstOrDefault();
        }
    }
}
#endif
