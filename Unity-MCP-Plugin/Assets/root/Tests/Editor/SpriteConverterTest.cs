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
using com.IvanMurzak.McpPlugin.Common.Reflection.Convertor;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Convertor;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Reflection.Convertor;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class SpriteConverterTest
    {
        private string _testFolder = "Assets/SpriteConverterTest";
        private string? _texturePath;
        private string? _spritePath;

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);

            AssetDatabase.CreateFolder("Assets", "SpriteConverterTest");
            _texturePath = $"{_testFolder}/TestTexture.png";

            // Create a simple texture
            var texture = new Texture2D(64, 64);
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(_texturePath, bytes);
            AssetDatabase.Refresh();

            // Import as Sprite
            var importer = AssetImporter.GetAtPath(_texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            _spritePath = _texturePath; // Sprite is inside the texture asset
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testFolder))
            {
                AssetDatabase.DeleteAsset(_testFolder);
            }
        }

        [Test]
        public void TestSpritePopulation()
        {
            var reflector = new Reflector();

            // Match UnityMcpPlugin.CreateDefaultReflector
            reflector.Convertors.Remove<GenericReflectionConvertor<object>>();
            reflector.Convertors.Add(new UnityGenericReflectionConvertor<object>());

            // Register converters in the order they are in UnityMcpPlugin.Converters.cs
            // Assets
            reflector.Convertors.Add(new UnityEngine_Material_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Sprite_ReflectionConvertor());

            // Fallback
            reflector.Convertors.Add(new UnityEngine_Object_ReflectionConvertor());

            // Create a dummy object to populate
            var container = new SpriteContainer();

            // Create SerializedMember for the sprite field
            // We use AssetObjectRef pointing to the texture path
            var assetRef = new AssetObjectRef() { AssetPath = _spritePath };

            // Manually serialize AssetObjectRef to JsonElement to ensure valueJsonElement is populated
            // This mimics how data comes from the wire (JSON)
            var json = System.Text.Json.JsonSerializer.Serialize(assetRef, reflector.JsonSerializerOptions);
            var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json, reflector.JsonSerializerOptions);

            var spriteMember = new SerializedMember
            {
                name = "spriteField",
                typeName = typeof(Sprite).AssemblyQualifiedName,
                valueJsonElement = jsonElement
            };

            var spritePropertyMember = new SerializedMember
            {
                name = "spriteProperty",
                typeName = typeof(Sprite).AssemblyQualifiedName,
                valueJsonElement = jsonElement
            };

            // Try to populate
            object? obj = container;
            bool result = false;

            result = reflector.TryPopulate(ref obj, new SerializedMember
            {
                typeName = typeof(SpriteContainer).AssemblyQualifiedName,
                fields = new SerializedMemberList { spriteMember },
                props = new SerializedMemberList { spritePropertyMember }
            });

            // Assert.IsTrue(result, "Population should succeed");
            Assert.IsNotNull((obj as SpriteContainer)?.spriteField, "Sprite field should be populated");
            Assert.AreEqual("TestTexture", (obj as SpriteContainer)?.spriteField?.name);

            Assert.IsNotNull((obj as SpriteContainer)?.spriteProperty, "Sprite property should be populated");
            Assert.AreEqual("TestTexture", (obj as SpriteContainer)?.spriteProperty?.name);
        }

        public class SpriteContainer
        {
            public Sprite? spriteField;
            public Sprite? spriteProperty { get; set; }
        }
    }
}
