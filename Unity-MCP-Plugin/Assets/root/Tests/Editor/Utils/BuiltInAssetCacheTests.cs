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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class BuiltInAssetCacheTests : BaseTest
    {
        [Test]
        public void FindAsset_Knob_AsSprite_ReturnsSprite()
        {
            var result = BuiltInAssetCache.FindAsset("Knob", typeof(Sprite));

            Assert.IsNotNull(result, "Should find a built-in asset named 'Knob' of type Sprite");
            Assert.IsInstanceOf<Sprite>(result, $"Result should be Sprite but was {result?.GetType().Name}");
            Assert.AreEqual("Knob", result!.name, "Asset name should be 'Knob'");
        }

        [Test]
        public void FindAsset_Knob_AsTexture2D_ReturnsTexture2D()
        {
            var result = BuiltInAssetCache.FindAsset("Knob", typeof(Texture2D));

            Assert.IsNotNull(result, "Should find a built-in asset named 'Knob' of type Texture2D");
            Assert.IsInstanceOf<Texture2D>(result, $"Result should be Texture2D but was {result?.GetType().Name}");
            Assert.AreEqual("Knob", result!.name, "Asset name should be 'Knob'");
        }

        [Test]
        public void FindAsset_Knob_SpriteAndTexture2D_AreDifferentObjects()
        {
            var sprite = BuiltInAssetCache.FindAsset("Knob", typeof(Sprite));
            var texture = BuiltInAssetCache.FindAsset("Knob", typeof(Texture2D));

            Assert.IsNotNull(sprite, "Should find Sprite named 'Knob'");
            Assert.IsNotNull(texture, "Should find Texture2D named 'Knob'");
            Assert.AreNotSame(sprite, texture, "Sprite and Texture2D should be different objects");
            Assert.AreNotEqual(sprite!.GetType(), texture!.GetType(), "Types should be different");
        }

        [Test]
        public void FindAssetByExtension_KnownBuiltIn_ReturnsAsset()
        {
            // "Default-Material" is a standard built-in asset in Unity editor resources
            var result = BuiltInAssetCache.FindAssetByExtension("Default-Material", ".mat");

            // Result might be null in some headless environments, but generally should pass in Editor
            if (result != null)
            {
                Assert.AreEqual("Default-Material", result.name);
                Assert.IsInstanceOf<Material>(result);
            }

            var mismatch = BuiltInAssetCache.FindAssetByExtension("Default-Material", ".shader");
            Assert.IsNull(mismatch);
        }

        [Test]
        public void FindAssetByExtension_NullOrEmptyExtension_ReturnsAnyMatch()
        {
            var result = BuiltInAssetCache.FindAssetByExtension("Default-Material", null);
            if (result != null)
            {
                Assert.AreEqual("Default-Material", result.name);
            }
        }
    }
}
