#nullable enable
using System.Collections;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class DataFieldScriptableObjectPopulationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        [UnityTest]
        public IEnumerator Populate_All_Fields_SO_Test()
        {
            Debug.Log("[DataFieldScriptableObjectPopulationTests] Running field population test for SO.");
            // Executors for creating assets
            var materialEx = new CreateMaterialExecutor("TestMaterialSO.mat", "Standard", "Assets", "Unity-MCP-Test", "DataFieldSOPopulation");
            var textureEx = new CreateTextureExecutor("TestTextureSO.png", Color.magenta, 64, 64, "Assets", "Unity-MCP-Test", "DataFieldSOPopulation");
            var spriteEx = new CreateSpriteExecutor("TestSpriteSO.png", Color.green, 64, 64, "Assets", "Unity-MCP-Test", "DataFieldSOPopulation");

            // The SO we are testing
            var soEx = new CreateScriptableObjectExecutor<DataFieldPopulationTestScriptableObject>("TestSOField.asset", "Assets", "Unity-MCP-Test", "DataFieldSOPopulation");

            var prefabSourceGoEx = new CreateGameObjectExecutor("PrefabSourceSO");
            var prefabEx = new CreatePrefabExecutor("TestPrefabSO.prefab", null, "Assets", "Unity-MCP-Test", "DataFieldSOPopulation");

            // Target GameObject for reference
            var targetGoName = "TargetGOSO";
            var targetGoEx = new CreateGameObjectExecutor(targetGoName);

            // Validation Executor
            var validateEx = new LazyNodeExecutor();
            validateEx.SetAction<object?>((input) =>
            {
                var so = soEx.Asset;
                Assert.IsNotNull(so, "ScriptableObject should exist");

                Assert.AreEqual(42, so!.intField, "intField not populated");
                Assert.AreEqual("Hello World", so.stringField, "stringField not populated");

                Assert.IsNotNull(so.materialField, "Material should be populated");
                Assert.AreEqual(materialEx.Asset!.name, so.materialField.name);

                Assert.IsNotNull(so.gameObjectField, "GameObject should be populated");
                Assert.AreEqual(targetGoEx.GameObject!.name, so.gameObjectField.name);

                Assert.IsNotNull(so.textureField, "Texture should be populated");
                Assert.AreEqual(textureEx.Asset!.name, so.textureField.name);

                Assert.IsNotNull(so.spriteField, "Sprite should be populated");
                Assert.AreEqual(spriteEx.Sprite!.name, so.spriteField.name);

                Assert.IsNotNull(so.scriptableObjectField, "SO should be populated");
                Assert.AreEqual(soEx.Asset!.name, so.scriptableObjectField.name);

                Assert.IsNotNull(so.prefabField, "Prefab should be populated");
                Assert.AreEqual(prefabEx.Asset!.name, so.prefabField.name);

                Assert.IsNotNull(so.materialArray, "Material array should be populated");
                Assert.AreEqual(2, so.materialArray.Length);
                Assert.AreEqual(materialEx.Asset.name, so.materialArray[0].name);

                Assert.IsNotNull(so.gameObjectArray, "GameObject array should be populated");
                Assert.AreEqual(2, so.gameObjectArray!.Length);

                Assert.IsNotNull(so.materialList, "Material list should be populated");
                Assert.AreEqual(2, so.materialList.Count);
                Assert.AreEqual(materialEx.Asset.name, so.materialList[0].name);

                Assert.IsNotNull(so.gameObjectList, "GameObject list should be populated");
                Assert.AreEqual(2, so.gameObjectList.Count);
            });

            // Chain creation
            var modifyEx = new DynamicCallToolExecutor(
                typeof(Tool_Assets).GetMethod(nameof(Tool_Assets.Modify)),
                () =>
                {
                    var plugin = UnityMcpPlugin.Instance;
                    var mcpInstance = plugin?.McpPluginInstance;
                    var manager = mcpInstance?.McpManager;
                    var reflector = manager?.Reflector;

                    if (reflector == null)
                    {
                        Debug.LogError("[DataFieldScriptableObjectPopulationTests] Reflector is null! Cannot proceed with serialization.");
                        return "{}";
                    }

                    var matRef = new AssetObjectRef() { AssetPath = materialEx.AssetPath };
                    var texRef = new AssetObjectRef() { AssetPath = textureEx.AssetPath };
                    var soRef = new AssetObjectRef() { AssetPath = soEx.AssetPath };
                    var prefabRef = new AssetObjectRef() { AssetPath = prefabEx.AssetPath };
                    var goRef = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var spriteRef = new AssetObjectRef() { AssetPath = spriteEx.AssetPath };

                    var soModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "DataFieldPopulationTestScriptableObject",
                        type: typeof(DataFieldPopulationTestScriptableObject),
                        value: new AssetObjectRef() { AssetPath = soEx.AssetPath }
                    );

                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "spriteField", type: typeof(Sprite), value: spriteRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialField", type: typeof(Material), value: matRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectField", type: typeof(GameObject), value: goRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "textureField", type: typeof(Texture2D), value: texRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "scriptableObjectField", type: typeof(DataFieldPopulationTestScriptableObject), value: soRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "prefabField", type: typeof(GameObject), value: prefabRef));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "intField", type: typeof(int), value: 42));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "stringField", type: typeof(string), value: "Hello World"));

                    var matRefArrayItem = new AssetObjectRef(materialEx.AssetPath!);
                    var goRefArrayItem = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var prefabRefArrayItem = new AssetObjectRef(prefabEx.AssetPath!);

                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialArray", type: typeof(Material[]), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectArray", type: typeof(GameObject[]), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialList", type: typeof(List<Material>), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    soModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectList", type: typeof(List<GameObject>), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    var assetRefJson = System.Text.Json.JsonSerializer.Serialize(new AssetObjectRef() { AssetPath = soEx.AssetPath }, options);
                    var contentJson = System.Text.Json.JsonSerializer.Serialize(soModification, options);

                    var json = JsonTestUtils.Fill(@"{
                            ""assetRef"": {assetRef},
                            ""content"": {content}
                        }",
                        new Dictionary<string, object?>
                        {
                            { "{assetRef}", assetRefJson },
                            { "{content}", contentJson }
                        });

                    Debug.Log($"[DataFieldScriptableObjectPopulationTests] JSON Input: {json}");
                    return json;
                }
            );

            modifyEx.AddChild(validateEx);
            soEx.AddChild(modifyEx);
            targetGoEx.AddChild(soEx);

            materialEx
                .Nest(textureEx)
                .Nest(spriteEx)
                .Nest(prefabSourceGoEx)
                .Nest(prefabEx)
                .Nest(targetGoEx);

            materialEx.Execute();
            yield return null;
        }
    }
}
