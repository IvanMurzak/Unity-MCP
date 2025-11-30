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
    public class DataPropertyPopulationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        [UnityTest]
        public IEnumerator Populate_All_Properties_Test()
        {
            Debug.Log("[DataPropertyPopulationTests] Running property population test.");
            // Executors for creating assets
            var materialEx = new CreateMaterialExecutor("TestMaterialProp.mat", "Standard", "Assets", "Unity-MCP-Test", "DataPropertyPopulation");
            var textureEx = new CreateTextureExecutor("TestTextureProp.png", Color.magenta, 64, 64, "Assets", "Unity-MCP-Test", "DataPropertyPopulation");
            var spriteEx = new CreateSpriteExecutor("TestSpriteProp.png", Color.green, 64, 64, "Assets", "Unity-MCP-Test", "DataPropertyPopulation");
            var soEx = new CreateScriptableObjectExecutor<DataFieldPopulationTestScriptableObject>("TestSOProp.asset", "Assets", "Unity-MCP-Test", "DataPropertyPopulation");

            var prefabSourceGoEx = new CreateGameObjectExecutor("PrefabSourceProp");
            var prefabEx = new CreatePrefabExecutor("TestPrefabProp.prefab", null, "Assets", "Unity-MCP-Test", "DataPropertyPopulation");

            // Target GameObject
            var targetGoName = "TargetGOProp";
            var targetGoRef = new GameObjectRef() { Name = targetGoName };
            var targetGoEx = new CreateGameObjectExecutor(targetGoName);
            var addCompEx = new AddComponentExecutor<DataPropertyPopulationTestScript>(targetGoRef);

            // Validation Executor
            var validateEx = new LazyNodeExecutor();
            validateEx.SetAction<object?>((input) =>
            {
                var comp = addCompEx.Component;
                Assert.IsNotNull(comp, "Component should exist");

                Assert.AreEqual(42, comp!.intProperty, "intProperty not populated");
                Assert.AreEqual("Hello World", comp.stringProperty, "stringProperty not populated");

                Assert.IsNotNull(comp.materialProperty, "Material should be populated");
                Assert.AreEqual(materialEx.Asset!.name, comp.materialProperty.name);

                Assert.IsNotNull(comp.gameObjectProperty, "GameObject should be populated");
                Assert.AreEqual(targetGoEx.GameObject!.name, comp.gameObjectProperty.name);

                Assert.IsNotNull(comp.textureProperty, "Texture should be populated");
                Assert.AreEqual(textureEx.Asset!.name, comp.textureProperty.name);

                Assert.IsNotNull(comp.spriteProperty, "Sprite should be populated");
                Assert.AreEqual(spriteEx.Sprite!.name, comp.spriteProperty.name);

                Assert.IsNotNull(comp.scriptableObjectProperty, "SO should be populated");
                Assert.AreEqual(soEx.Asset!.name, comp.scriptableObjectProperty.name);

                Assert.IsNotNull(comp.prefabProperty, "Prefab should be populated");
                Assert.AreEqual(prefabEx.Asset!.name, comp.prefabProperty.name);

                Assert.IsNotNull(comp.materialArrayProperty, "Material array should be populated");
                Assert.AreEqual(2, comp.materialArrayProperty.Length);
                Assert.AreEqual(materialEx.Asset.name, comp.materialArrayProperty[0].name);

                Assert.IsNotNull(comp.gameObjectArrayProperty, "GameObject array should be populated");
                Assert.AreEqual(2, comp.gameObjectArrayProperty!.Length);

                Assert.IsNotNull(comp.materialListProperty, "Material list should be populated");
                Assert.AreEqual(2, comp.materialListProperty.Count);
                Assert.AreEqual(materialEx.Asset.name, comp.materialListProperty[0].name);

                Assert.IsNotNull(comp.gameObjectListProperty, "GameObject list should be populated");
                Assert.AreEqual(2, comp.gameObjectListProperty.Count);
            });

            // Chain creation
            var modifyEx = new DynamicCallToolExecutor(
                typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Modify)),
                () =>
                {
                    var plugin = UnityMcpPlugin.Instance;
                    var mcpInstance = plugin?.McpPluginInstance;
                    var manager = mcpInstance?.McpManager;
                    var reflector = manager?.Reflector;

                    if (reflector == null)
                    {
                        Debug.LogError("[DataPropertyPopulationTests] Reflector is null! Cannot proceed with serialization.");
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
                        name: "TargetGOProp",
                        type: typeof(GameObject),
                        value: new GameObjectRef(targetGoEx.GameObject!.GetInstanceID())
                    );

                    var componentModification = SerializedMember.FromValue(
                        reflector: reflector,
                        name: "DataPropertyPopulationTestScript",
                        type: typeof(DataPropertyPopulationTestScript),
                        value: new ComponentRef(addCompEx.Component!.GetInstanceID())
                    );

                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "spriteProperty", type: typeof(Sprite), value: spriteRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialProperty", type: typeof(Material), value: matRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectProperty", type: typeof(GameObject), value: goRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "textureProperty", type: typeof(Texture2D), value: texRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "scriptableObjectProperty", type: typeof(DataFieldPopulationTestScriptableObject), value: soRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "prefabProperty", type: typeof(GameObject), value: prefabRef));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "intProperty", type: typeof(int), value: 42));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "stringProperty", type: typeof(string), value: "Hello World"));

                    var matRefArrayItem = new AssetObjectRef(materialEx.AssetPath!);
                    var goRefArrayItem = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var prefabRefArrayItem = new AssetObjectRef(prefabEx.AssetPath!);

                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialArrayProperty", type: typeof(Material[]), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectArrayProperty", type: typeof(GameObject[]), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialListProperty", type: typeof(List<Material>), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    componentModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectListProperty", type: typeof(List<GameObject>), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

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

                    Debug.Log($"[DataPropertyPopulationTests] JSON Input: {json}");
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
