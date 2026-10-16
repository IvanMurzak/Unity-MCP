/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Sprite arguments and members accept what an agent naturally has: the texture (<c>unity.asset("Assets/…/grass.png")</c>
    /// returns the Texture2D main asset) or its path; <c>"Assets/…/sheet.png#grass_3"</c> picks one sprite of a sheet.
    /// </summary>
    public sealed class UnityEngine_Sprite_Adapter : RqlTypeAdapter<Sprite>
    {
        public override string Description =>
            "Sprite: write as a ref, the texture (unity.asset(\"Assets/…/grass.png\")), its path, or \"Assets/…/sheet.png#spriteName\" for one sprite of a sheet";

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            string? path, name = null;
            switch (value)
            {
                case Texture2D texture when texture != null:
                    path = AssetDatabase.GetAssetPath(texture);
                    if (string.IsNullOrEmpty(path)) { reason = $"texture '{texture.name}' is not an asset"; return AdapterOutcome.Failed; }
                    break;
                case string text:
                    if (!UnityEngine_Object_Adapter.TryAssetPath(text, out path)) return AdapterOutcome.NotHandled;
                    int hash = path.LastIndexOf('#');
                    if (hash > 0) { name = path.Substring(hash + 1); path = path.Substring(0, hash); }
                    break;
                default:
                    return AdapterOutcome.NotHandled;
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                reason = AssetDatabase.LoadMainAssetAtPath(path) == null
                    ? $"no asset at '{path}'"
                    : $"'{path}' has no sprites (set the texture's Texture Type to Sprite (2D and UI))";
                return AdapterOutcome.Failed;
            }
            Sprite? sprite;
            if (name != null)
            {
                sprite = sprites.FirstOrDefault(s => s.name == name);
                if (sprite == null)
                {
                    reason = $"'{path}' has no sprite '{name}' (sprites: {string.Join(", ", sprites.Select(s => s.name).Take(20))})";
                    return AdapterOutcome.Failed;
                }
            }
            else
            {
                // A single-sprite texture names its sprite after the file; a sheet without #name takes its first sprite.
                sprite = sprites.FirstOrDefault(s => s.name == System.IO.Path.GetFileNameWithoutExtension(path)) ?? sprites[0];
            }
            result = sprite;
            return AdapterOutcome.Ok;
        }
    }
}
