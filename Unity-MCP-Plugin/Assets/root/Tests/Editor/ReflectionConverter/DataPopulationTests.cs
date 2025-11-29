#nullable enable
using System.Collections;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Reflection.Convertor;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class DataPopulationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        [UnityTest]
        public IEnumerator Populate_All_Types_Test()
        {
            Debug.Log("[DataPopulationTests] Running updated test version.");
            // Executors for creating assets
            var materialEx = new CreateMaterialExecutor("TestMaterial.mat", "Standard", "Assets", "Unity-MCP-Test", "DataPopulation");
            var textureEx = new CreateTextureExecutor("TestTexture.png", Color.magenta, 64, 64, "Assets", "Unity-MCP-Test", "DataPopulation");
            var spriteEx = new CreateSpriteExecutor("TestSprite.png", Color.green, 64, 64, "Assets", "Unity-MCP-Test", "DataPopulation");
            var soEx = new CreateScriptableObjectExecutor<DataFieldPopulationTestScriptableObject>("TestSO.asset", "Assets", "Unity-MCP-Test", "DataPopulation");

            var prefabSourceGoEx = new CreateGameObjectExecutor("PrefabSource");
            var prefabEx = new CreatePrefabExecutor("TestPrefab.prefab", null, "Assets", "Unity-MCP-Test", "DataPopulation");

            // Target GameObject
            var targetGoName = "TargetGO";
            var targetGoRef = new GameObjectRef() { Name = targetGoName };
            var targetGoEx = new CreateGameObjectExecutor(targetGoName);
            var addCompEx = new AddComponentExecutor<DataFieldPopulationTestScript>(targetGoRef);

            // Validation Executor
            var validateEx = new LazyNodeExecutor();
            validateEx.SetAction<object?>((input) =>
            {
                var comp = addCompEx.Component;
                Assert.IsNotNull(comp, "Component should exist");

                Assert.AreEqual(42, comp!.intField, "intField not populated");
                Assert.AreEqual("Hello World", comp.stringField, "stringField not populated");

                Assert.IsNotNull(comp.materialField, "Material should be populated");
                Assert.AreEqual(materialEx.Asset!.name, comp.materialField.name);

                Assert.IsNotNull(comp.gameObjectField, "GameObject should be populated");
                Assert.AreEqual(targetGoEx.GameObject!.name, comp.gameObjectField.name);

                Assert.IsNotNull(comp.textureField, "Texture should be populated");
                Assert.AreEqual(textureEx.Asset!.name, comp.textureField.name);

                Assert.IsNotNull(comp.spriteField, "Sprite should be populated");
                Assert.AreEqual(spriteEx.Sprite!.name, comp.spriteField.name);

                Assert.IsNotNull(comp.scriptableObjectField, "SO should be populated");
                Assert.AreEqual(soEx.Asset!.name, comp.scriptableObjectField.name);

                Assert.IsNotNull(comp.prefabField, "Prefab should be populated");
                Assert.AreEqual(prefabEx.Asset!.name, comp.prefabField.name);

                Assert.IsNotNull(comp.materialArray, "Material array should be populated");
                Assert.AreEqual(2, comp.materialArray.Length);
                Assert.AreEqual(materialEx.Asset.name, comp.materialArray[0].name);

                Assert.IsNotNull(comp.gameObjectArray, "GameObject array should be populated");
                Assert.AreEqual(2, comp.gameObjectArray!.Length);

                Assert.IsNotNull(comp.materialList, "Material list should be populated");
                Assert.AreEqual(2, comp.materialList.Count);
                Assert.AreEqual(materialEx.Asset.name, comp.materialList[0].name);

                Assert.IsNotNull(comp.gameObjectList, "GameObject list should be populated");
                Assert.AreEqual(2, comp.gameObjectList.Count);
            });

            // Chain creation
            var modifyEx = new DynamicCallToolExecutor(
                typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Modify)),
                () =>
                {
                    var plugin = UnityMcpPlugin.Instance;
                    Debug.Log($"[DataPopulationTests] Plugin: {plugin}");
                    var mcpInstance = plugin?.McpPluginInstance;
                    Debug.Log($"[DataPopulationTests] McpInstance: {mcpInstance}");
                    var manager = mcpInstance?.McpManager;
                    Debug.Log($"[DataPopulationTests] Manager: {manager}");
                    var reflector = manager?.Reflector;
                    Debug.Log($"[DataPopulationTests] Reflector: {reflector}");

                    if (reflector == null)
                    {
                        Debug.LogError("[DataPopulationTests] Reflector is null! Cannot proceed with serialization.");
                        return "{}";
                    }

                    var matRef = new AssetObjectRef() { AssetPath = materialEx.AssetPath };
                    var texRef = new AssetObjectRef() { AssetPath = textureEx.AssetPath };
                    var soRef = new AssetObjectRef() { AssetPath = soEx.AssetPath };
                    var prefabRef = new AssetObjectRef() { AssetPath = prefabEx.AssetPath };
                    var goRef = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var spriteRef = new AssetObjectRef() { AssetPath = spriteEx.AssetPath };

                    var goModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "TargetGO",
                        type: typeof(GameObject),
                        value: new GameObjectRef(targetGoEx.GameObject!.GetInstanceID())
                    );

                    var componentModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "DataFieldPopulationTestScript",
                        type: typeof(DataFieldPopulationTestScript),
                        value: new ComponentRef(addCompEx.Component!.GetInstanceID())
                    );

                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "spriteField", type: typeof(Sprite), value: spriteRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialField", type: typeof(Material), value: matRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectField", type: typeof(GameObject), value: goRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "textureField", type: typeof(Texture2D), value: texRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "scriptableObjectField", type: typeof(DataFieldPopulationTestScriptableObject), value: soRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "prefabField", type: typeof(GameObject), value: prefabRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "intField", type: typeof(int), value: 42));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "stringField", type: typeof(string), value: "Hello World"));

                    var matRefArrayItem = new AssetObjectRef(materialEx.AssetPath!);
                    var goRefArrayItem = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var prefabRefArrayItem = new AssetObjectRef(prefabEx.AssetPath!);

                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialArray", type: typeof(Material[]), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectArray", type: typeof(GameObject[]), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialList", type: typeof(List<Material>), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectList", type: typeof(List<GameObject>), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    goModification.AddField(componentModification);

                    var gameObjectDiffs = new SerializedMemberList { goModification };

                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    var gameObjectRefsJson = System.Text.Json.JsonSerializer.Serialize(new GameObjectRef[] { targetGoRef }, options);
                    var gameObjectDiffsJson = System.Text.Json.JsonSerializer.Serialize(gameObjectDiffs, options);

                    var json = JsonTestUtils.Fill(@"{
                            ""gameObjectRefs"": {gameObjectRefs},
                            ""gameObjectDiffs"": {gameObjectDiffs}
                        }",
                        new Dictionary<string, object?>
                        {
                            { "{gameObjectRefs}", gameObjectRefsJson },
                            { "{gameObjectDiffs}", gameObjectDiffsJson }
                        });

                    Debug.Log($"[DataPopulationTests] JSON Input: {json}");
                    return json;
                }
            );

            modifyEx.AddChild(validateEx);
            addCompEx.AddChild(modifyEx);
            targetGoEx.AddChild(addCompEx);

            materialEx
                .Nest(textureEx)
                .Nest(spriteEx)
                .Nest(soEx)
                .Nest(prefabSourceGoEx)
                .Nest(prefabEx)
                .Nest(targetGoEx);

            materialEx.Execute();
            yield return null;
        }
    }
}
