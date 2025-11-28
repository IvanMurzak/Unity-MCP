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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateSpriteExecutor : CreateTextureExecutor
    {
        public Sprite Sprite { get; private set; } = null!;

        public CreateSpriteExecutor(string assetName, params string[] folders) : base(assetName, folders)
        {
            SetAction<Texture2D, Sprite>((texture) =>
            {
                if (texture == null) throw new System.ArgumentNullException(nameof(texture));

                Debug.Log($"Converting Texture to Sprite: {AssetPath}");

                var importer = AssetImporter.GetAtPath(AssetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath);

                if (Sprite == null)
                {
                    Debug.LogError($"Failed to load created sprite at {AssetPath}");
                }
                else
                {
                    Debug.Log($"Created Sprite: {AssetPath}");
                }

                return Sprite;
            });
        }
    }
}
