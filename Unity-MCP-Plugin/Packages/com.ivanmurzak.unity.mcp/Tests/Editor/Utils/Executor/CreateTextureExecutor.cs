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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateTextureExecutor : BaseCreateAssetExecutor<Texture2D>
    {
        public CreateTextureExecutor(string assetName, Color color, int width = 64, int height = 64, params string[] folders) : base(assetName, folders)
        {
            SetAction(() =>
            {
                Debug.Log($"Creating Texture: {AssetPath} ({width}x{height}) with color {color}");

                var texture = new Texture2D(width, height);
                // Fill with some color
                var colors = new Color[width * height];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = color;
                }
                texture.SetPixels(colors);
                texture.Apply();

                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(AssetPath, bytes);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Asset = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath);

                if (Asset == null)
                {
                    Debug.LogError($"Failed to load created texture at {AssetPath}");
                }
                else
                {
                    Debug.Log($"Created Texture: {AssetPath}");
                }

                return Asset;
            });
        }
        protected override void PostExecute(object? input)
        {
            base.PostExecute(input);
            Object.DestroyImmediate(Asset);
        }
    }
}
