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
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// A view nothing drew into reads back all black with no error from Unity (a headless Game View that no camera
    /// renders to); screenshot-game-view and screenshot-scene-view check for it. CPU-only, so it runs in every editor.
    /// </summary>
    public class ScreenshotBlackFrameTests : BaseTest
    {
        static Texture2D Filled(Color32 color, Color32? onePixel = null)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGB24, false);
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            if (onePixel is { } pixel)
                pixels[7] = pixel;
            texture.SetPixels32(pixels);
            return texture;
        }

        [Test]
        public void IsAllBlack_BlackFrame_IsTrue()
        {
            var texture = Filled(new Color32(0, 0, 0, 255));
            try { Assert.IsTrue(Tool_Screenshot.IsAllBlack(texture)); }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void IsAllBlack_OneNonBlackPixel_IsFalse()
        {
            var texture = Filled(new Color32(0, 0, 0, 255), new Color32(0, 0, 1, 255));
            try { Assert.IsFalse(Tool_Screenshot.IsAllBlack(texture)); }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void IsAllBlack_DarkGrayFrame_IsFalse()
        {
            var texture = Filled(new Color32(8, 8, 8, 255));
            try { Assert.IsFalse(Tool_Screenshot.IsAllBlack(texture)); }
            finally { Object.DestroyImmediate(texture); }
        }
    }
}
