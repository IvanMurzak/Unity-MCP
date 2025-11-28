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
    public class DataPopulationTests
    {
        [UnityTest]
        public IEnumerator Populate_All_Types_Test()
        {
            // Executors for creating assets
            var materialEx = new CreateMaterialExecutor("TestMaterial.mat", "Standard", "Assets", "Unity-MCP-Test", "DataPopulation");
            var textureEx = new CreateTextureExecutor("TestTexture.png", "Assets", "Unity-MCP-Test", "DataPopulation");
            var soEx = new CreateScriptableObjectExecutor<DataPopulationTestScriptableObject>("TestSO.asset", "Assets", "Unity-MCP-Test", "DataPopulation");

            var prefabSourceGoEx = new CreateGameObjectExecutor("PrefabSource");
            var prefabEx = new CreatePrefabExecutor("TestPrefab.prefab", null, "Assets", "Unity-MCP-Test", "DataPopulation");

            // Target GameObject
            var targetGoName = "TargetGO";
            var targetGoRef = new GameObjectRef() { Name = targetGoName };
            var targetGoEx = new CreateGameObjectExecutor(targetGoName);
            var addCompEx = new AddComponentExecutor<DataPopulationTestScript>(targetGoRef);

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

                Assert.IsNotNull(comp.scriptableObjectField, "SO should be populated");
                Assert.AreEqual(soEx.Asset!.name, comp.scriptableObjectField.name);

                Assert.IsNotNull(comp.prefabField, "Prefab should be populated");
                Assert.AreEqual(prefabEx.Asset!.name, comp.prefabField.name);

                Assert.IsNotNull(comp.materialArray, "Material array should be populated");
                Assert.AreEqual(2, comp.materialArray.Length);
                Assert.AreEqual(materialEx.Asset.name, comp.materialArray[0].name);

                Assert.IsNotNull(comp.gameObjectArray, "GameObject array should be populated");
                Assert.AreEqual(2, comp.gameObjectArray!.Length);
            });

            // Chain creation
            var modifyEx = new DynamicCallToolExecutor(
                typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Modify)),
                () =>
                {
                    var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                    var matRef = new AssetObjectRef() { AssetPath = materialEx.AssetPath };
                    var texRef = new AssetObjectRef() { AssetPath = textureEx.AssetPath };
                    var soRef = new AssetObjectRef() { AssetPath = soEx.AssetPath };
                    var prefabRef = new AssetObjectRef() { AssetPath = prefabEx.AssetPath };
                    var goRef = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());

                    var goModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "TargetGO",
                        type: typeof(GameObject),
                        value: new GameObjectRef(targetGoEx.GameObject!.GetInstanceID())
                    );

                    var componentModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "DataPopulationTestScript",
                        type: typeof(DataPopulationTestScript),
                        value: new ComponentRef(addCompEx.Component!.GetInstanceID())
                    );

                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialField", type: typeof(Material), value: matRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectField", type: typeof(GameObject), value: goRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "textureField", type: typeof(Texture2D), value: texRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "scriptableObjectField", type: typeof(DataPopulationTestScriptableObject), value: soRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "prefabField", type: typeof(GameObject), value: prefabRef));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "intField", type: typeof(int), value: 42));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "stringField", type: typeof(string), value: "Hello World"));

                    var matRefArrayItem = new ObjectRef(materialEx.Asset!.GetInstanceID());
                    var goRefArrayItem = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var prefabRefArrayItem = new ObjectRef(prefabEx.Asset!.GetInstanceID());

                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "materialArray", type: typeof(Material[]), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    componentModification.AddField(SerializedMember.FromValue(reflector: reflector, name: "gameObjectArray", type: typeof(GameObject[]), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

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
                .Nest(soEx)
                .Nest(prefabSourceGoEx)
                .Nest(prefabEx)
                .Nest(targetGoEx);

            materialEx.Execute();
            yield return null;
        }
    }
}
