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
    public class DataPropertyScriptableObjectPopulationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        [UnityTest]
        public IEnumerator Populate_All_Properties_SO_Test()
        {
            Debug.Log("[DataPropertyScriptableObjectPopulationTests] Running property population test for SO.");
            // Executors for creating assets
            var materialEx = new CreateMaterialExecutor("TestMaterialSOProp.mat", "Standard", "Assets", "Unity-MCP-Test", "DataPropertySOPopulation");
            var textureEx = new CreateTextureExecutor("TestTextureSOProp.png", Color.magenta, 64, 64, "Assets", "Unity-MCP-Test", "DataPropertySOPopulation");
            var spriteEx = new CreateSpriteExecutor("TestSpriteSOProp.png", Color.green, 64, 64, "Assets", "Unity-MCP-Test", "DataPropertySOPopulation");

            // The SO we are testing
            var soEx = new CreateScriptableObjectExecutor<DataPropertyPopulationTestScriptableObject>("TestSOProp.asset", "Assets", "Unity-MCP-Test", "DataPropertySOPopulation");

            var prefabSourceGoEx = new CreateGameObjectExecutor("PrefabSourceSOProp");
            var prefabEx = new CreatePrefabExecutor("TestPrefabSOProp.prefab", null, "Assets", "Unity-MCP-Test", "DataPropertySOPopulation");

            // Target GameObject for reference
            var targetGoName = "TargetGOSOProp";
            var targetGoEx = new CreateGameObjectExecutor(targetGoName);

            // Validation Executor
            var validateEx = new LazyNodeExecutor();
            validateEx.SetAction<object?>((input) =>
            {
                var so = soEx.Asset;
                Assert.IsNotNull(so, "ScriptableObject should exist");

                Assert.AreEqual(42, so!.intProperty, "intProperty not populated");
                Assert.AreEqual("Hello World", so.stringProperty, "stringProperty not populated");

                Assert.IsNotNull(so.materialProperty, "Material should be populated");
                Assert.AreEqual(materialEx.Asset!.name, so.materialProperty.name);

                Assert.IsNotNull(so.gameObjectProperty, "GameObject should be populated");
                Assert.AreEqual(targetGoEx.GameObject!.name, so.gameObjectProperty.name);

                Assert.IsNotNull(so.textureProperty, "Texture should be populated");
                Assert.AreEqual(textureEx.Asset!.name, so.textureProperty.name);

                Assert.IsNotNull(so.spriteProperty, "Sprite should be populated");
                Assert.AreEqual(spriteEx.Sprite!.name, so.spriteProperty.name);

                Assert.IsNotNull(so.scriptableObjectProperty, "SO should be populated");
                Assert.AreEqual(soEx.Asset!.name, so.scriptableObjectProperty.name);

                Assert.IsNotNull(so.prefabProperty, "Prefab should be populated");
                Assert.AreEqual(prefabEx.Asset!.name, so.prefabProperty.name);

                Assert.IsNotNull(so.materialArrayProperty, "Material array should be populated");
                Assert.AreEqual(2, so.materialArrayProperty.Length);
                Assert.AreEqual(materialEx.Asset.name, so.materialArrayProperty[0].name);

                Assert.IsNotNull(so.gameObjectArrayProperty, "GameObject array should be populated");
                Assert.AreEqual(2, so.gameObjectArrayProperty!.Length);

                Assert.IsNotNull(so.materialListProperty, "Material list should be populated");
                Assert.AreEqual(2, so.materialListProperty.Count);
                Assert.AreEqual(materialEx.Asset.name, so.materialListProperty[0].name);

                Assert.IsNotNull(so.gameObjectListProperty, "GameObject list should be populated");
                Assert.AreEqual(2, so.gameObjectListProperty.Count);
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
                        Debug.LogError("[DataPropertyScriptableObjectPopulationTests] Reflector is null! Cannot proceed with serialization.");
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
                        name: "DataPropertyPopulationTestScriptableObject",
                        type: typeof(DataPropertyPopulationTestScriptableObject),
                        value: new AssetObjectRef() { AssetPath = soEx.AssetPath }
                    );

                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "spriteProperty", type: typeof(Sprite), value: spriteRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialProperty", type: typeof(Material), value: matRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectProperty", type: typeof(GameObject), value: goRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "textureProperty", type: typeof(Texture2D), value: texRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "scriptableObjectProperty", type: typeof(DataPropertyPopulationTestScriptableObject), value: soRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "prefabProperty", type: typeof(GameObject), value: prefabRef));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "intProperty", type: typeof(int), value: 42));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "stringProperty", type: typeof(string), value: "Hello World"));

                    var matRefArrayItem = new AssetObjectRef(materialEx.AssetPath!);
                    var goRefArrayItem = new ObjectRef(targetGoEx.GameObject!.GetInstanceID());
                    var prefabRefArrayItem = new AssetObjectRef(prefabEx.AssetPath!);

                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialArrayProperty", type: typeof(Material[]), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectArrayProperty", type: typeof(GameObject[]), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "materialListProperty", type: typeof(List<Material>), value: new object[] { matRefArrayItem, matRefArrayItem }));
                    soModification.AddProperty(SerializedMember.FromValue(reflector: reflector, name: "gameObjectListProperty", type: typeof(List<GameObject>), value: new object[] { goRefArrayItem, prefabRefArrayItem }));

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

                    Debug.Log($"[DataPropertyScriptableObjectPopulationTests] JSON Input: {json}");
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
