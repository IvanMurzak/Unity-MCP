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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Reflection.Converter;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class SpriteConverterTest
    {
        [Test]
        public void TestSpritePopulation()
        {
            var spriteEx = new CreateSpriteExecutor("TestTexture.png", Color.red, 64, 64, "Assets", "SpriteConverterTest");

            spriteEx.AddChild(() =>
            {
                var reflector = new Reflector();

                // Match UnityMcpPlugin.CreateDefaultReflector
                reflector.Converters.Remove<GenericReflectionConverter<object>>();
                reflector.Converters.Add(new UnityGenericReflectionConverter<object>());

                // Register converters in the order they are in UnityMcpPlugin.Converters.cs
                // Assets
                reflector.Converters.Add(new UnityEngine_Material_ReflectionConverter());
                reflector.Converters.Add(new UnityEngine_Sprite_ReflectionConverter());

                // Fallback
                reflector.Converters.Add(new UnityEngine_Object_ReflectionConverter());

                // Create a dummy object to populate
                var container = new SpriteContainer();

                // Create SerializedMember for the sprite field
                // We use AssetObjectRef pointing to the texture path
                var assetRef = new AssetObjectRef() { AssetPath = spriteEx.AssetPath };

                // Manually serialize AssetObjectRef to JsonElement to ensure valueJsonElement is populated
                // This mimics how data comes from the wire (JSON)
                var json = reflector.JsonSerializer.Serialize(assetRef);
                var jsonElement = reflector.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

                var spriteMember = new SerializedMember
                {
                    name = nameof(SpriteContainer.spriteField),
                    typeName = typeof(Sprite).GetTypeId(), // typeof(Sprite).AssemblyQualifiedName,
                    valueJsonElement = jsonElement
                };

                var spritePropertyMember = new SerializedMember
                {
                    name = nameof(SpriteContainer.spriteProperty),
                    typeName = typeof(Sprite).GetTypeId(), // typeof(Sprite).AssemblyQualifiedName,
                    valueJsonElement = jsonElement
                };

                var data = new SerializedMember
                {
                    typeName = typeof(SpriteContainer).GetTypeId(), // typeof(SpriteContainer).AssemblyQualifiedName,
                    fields = new SerializedMemberList { spriteMember },
                    props = new SerializedMemberList { spritePropertyMember }
                };

                // Try to populate
                object? obj = container;
                var result = reflector.TryPopulate(ref obj, data);

                Assert.IsTrue(result, "Population should succeed");

                // Assert.IsTrue(result, "Population should succeed");
                Assert.IsNotNull((obj as SpriteContainer)?.spriteField, "Sprite field should be populated");
                Assert.AreEqual("TestTexture", (obj as SpriteContainer)?.spriteField?.name);

                Assert.IsNotNull((obj as SpriteContainer)?.spriteProperty, "Sprite property should be populated");
                Assert.AreEqual("TestTexture", (obj as SpriteContainer)?.spriteProperty?.name);
            });

            spriteEx.Execute();
        }

        public class SpriteContainer
        {
            public Sprite? spriteField;
            public Sprite? spriteProperty { get; set; }
        }
    }
}
