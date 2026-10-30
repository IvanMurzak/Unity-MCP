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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>
    /// RQL literal kinds (spec §4.5, RQL 2f4e263): a Material and a data ScriptableObject are values that a typed literal builds;
    /// GameObjects, components and other assets stay engine objects, refused with Unity's way to pass an existing one.
    /// </summary>
    public class RqlLiteralKindsTests : EditorApiTestBase
    {
        private static DataFieldPopulationTestScript Holder(string name = "Holder") =>
            new GameObject(name).AddComponent<DataFieldPopulationTestScript>();

        [Test]
        public void ATypedLiteralBuildsANewMaterial()
        {
            var renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            renderer.gameObject.name = "Crate";
            var before = renderer.sharedMaterial;
            AssertOk(M("objects | where name == \"Crate\" | get components | where $ is MeshRenderer | set sharedMaterial = {$type: \"Material\", color: \"#ff0000\"}"));
            var made = renderer.sharedMaterial;
            Assert.IsNotNull(made);
            Assert.AreNotSame(before, made);
            Assert.AreEqual(RqlEditorApi.DefaultShader(), made.shader, "the render pipeline's default shader");
            Assert.AreEqual(Color.red, made.color);
            Assert.IsFalse(EditorUtility.IsPersistent(made), "an object of the scene, saved with it");

            // Without $type the declared Material is built as well.
            var holder = Holder();
            AssertOk(M("objects | where name == \"Holder\" | get components | where $ is DataFieldPopulationTestScript | set materialField = {}"));
            Assert.IsNotNull(holder.materialField);
        }

        [Test]
        public void ATypedLiteralBuildsADataScriptableObject()
        {
            var holder = Holder();
            AssertOk(M("objects | where name == \"Holder\" | get components | where $ is DataFieldPopulationTestScript | " +
                       "set scriptableObjectField = {$type: \"DataFieldPopulationTestScriptableObject\", intField: 7, stringField: \"seven\"}"));
            var made = holder.scriptableObjectField as DataFieldPopulationTestScriptableObject;
            Assert.IsNotNull(made, holder.scriptableObjectField?.GetType().Name);
            Assert.AreEqual(7, made!.intField);
            Assert.AreEqual("seven", made.stringField);
        }

        [Test]
        public void DataScriptableObjectsAreValuesEditorOnesAreNot()
        {
            Assert.IsTrue(UnityEngine_Object_Adapter.IsDataScriptableObject(typeof(DataFieldPopulationTestScriptableObject)));
            Assert.IsFalse(UnityEngine_Object_Adapter.IsDataScriptableObject(typeof(ScriptableObject)), "abstract in practice: no data");
            Assert.IsFalse(UnityEngine_Object_Adapter.IsDataScriptableObject(typeof(EditorWindow)));
            Assert.IsFalse(UnityEngine_Object_Adapter.IsDataScriptableObject(typeof(SceneView)));
            Assert.IsFalse(UnityEngine_Object_Adapter.IsDataScriptableObject(typeof(GameObject)));
        }

        // RQL 3602d6a: a host array (another member's value) converts element by element into a typed List<T> member; before,
        // only RQL list literals did.
        [Test]
        public void AHostArrayIsAssignedToAListMember()
        {
            var a = new Material(Shader.Find("Standard")) { name = "A" };
            var b = new Material(Shader.Find("Standard")) { name = "B" };
            var holder = new GameObject("ListHolder").AddComponent<DataFieldPopulationTestScript>();
            holder.materialArray = new[] { a, b };
            AssertOk(M("objects | where name == \"ListHolder\" | get components | where $ is DataFieldPopulationTestScript | set materialList = materialArray"));
            CollectionAssert.AreEqual(new[] { a, b }, holder.materialList);
        }

        // RQL 3602d6a: a misspelt $type names the closest type.
        [Test]
        public void AMisspeltTypeNameGetsASuggestion()
        {
            new GameObject("Crate2", typeof(MeshRenderer));
            AssertFails(M("objects | where name == \"Crate2\" | get components | where $ is MeshRenderer | set sharedMaterial = {$type: \"Materal\"}"),
                "coercion-failed", "did you mean 'Material'");
        }

        [Test]
        public void AnAssetCannotHoldANewObject()
        {
            var asset = ScriptableObject.CreateInstance<DataFieldPopulationTestScriptableObject>();
            var path = Folder + "/Data.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssertFails(RqlUnity.Mutate($"unity.asset(\"{path}\") | set materialField = {{$type: \"Material\"}}"), "invalid-value",
                "a new Material written into the asset 'Data' would not be saved with it");
            Assert.IsNull(asset.materialField, "nothing changed");
            // In a list of the asset too: the nearest Unity object enclosing the write is the asset.
            AssertFails(RqlUnity.Mutate($"unity.asset(\"{path}\") | set materialList = [{{$type: \"Material\"}}]"), "invalid-value", "would not be saved with it");
        }

        [Test]
        public void TheOpenPrefabCannotHoldANewObject()
        {
            var path = Folder + "/Crate.prefab";
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            AssertOk(RqlUnity.Mutate($"editor | call OpenPrefab(\"{path}\")"));
            AssertFails(RqlUnity.Mutate("prefab | get components | where $ is MeshRenderer | set sharedMaterial = {$type: \"Material\"}"),
                "invalid-value", "written into the open prefab");
        }

        [Test]
        public void SceneObjectsAndOtherAssetsAreRefusedWithUnitysHint()
        {
            Holder();
            AssertFails(M("objects | where name == \"Holder\" | get components | where $ is DataFieldPopulationTestScript | set gameObjectField = {}"),
                "coercion-failed", "GameObject is an engine object and cannot be built from an object literal; use unity.find(\"Name\")");
            AssertFails(M("objects | where name == \"Holder\" | get components | where $ is DataFieldPopulationTestScript | set textureField = {}"),
                "coercion-failed", "use an asset path (\"Assets/…\")");
#if UNITY_6000_5_OR_NEWER
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
#else
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
            Assert.AreEqual(1, all.Length, "no GameObject was built");
        }

        [Test]
        public void ADryRunLeavesNoBuiltObjectBehind()
        {
            var holder = Holder();
            var before = Resources.FindObjectsOfTypeAll<DataFieldPopulationTestScriptableObject>().Length;
            AssertOk(RqlUnity.Mutate("objects | where name == \"Holder\" | get components | where $ is DataFieldPopulationTestScript | " +
                                     "set scriptableObjectField = {$type: \"DataFieldPopulationTestScriptableObject\"}", dryRun: true));
            Assert.IsNull(holder.scriptableObjectField);
            Assert.AreEqual(before, Resources.FindObjectsOfTypeAll<DataFieldPopulationTestScriptableObject>().Length, "the dry run's object was destroyed");
        }

        // RQL 2f4e263: list arithmetic and vector functions are core.
        [Test]
        public void VectorFunctionsAndListArithmeticInAMutation()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(3, 0, 4);
            var rock = new GameObject("Rock");
            rock.transform.position = new Vector3(1, 5, 2);
            AssertOk(M("objects | where name == \"Rock\" | set transform.position = [transform.position.x, 0, transform.position.z] * 2"));
            Assert.AreEqual(new Vector3(2, 0, 4), rock.transform.position);
            AssertOk(M("objects | where name == \"Rock\" | set transform.position = normalize(unity.find(\"Player\").transform.position)"));
            Assert.Less(Vector3.Distance(new Vector3(0.6f, 0, 0.8f), rock.transform.position), 1e-5f, rock.transform.position.ToString("F6"));
            Assert.AreEqual("5", Cell(Q("objects | where name == \"Player\" | select d = (distance(transform.position, [0, 0, 0]))")));
        }

        // RQL 2f4e263: a sort key that fails for some items still sorts them last, and now warns.
        [Test]
        public void AFailingSortKeyWarns()
        {
            new GameObject("A");
            new GameObject("B");
            var r = Q("objects | sort (1 / 0) | select name");
            AssertOk(r);
            StringAssert.Contains("sort key 1 failed for 2 of 2 item(s)", Table(r));
        }

        // Tool descriptions are attribute constants, so the shared rules are a copy: it must stay the bundled runtime's text.
        [Test]
        public void TheGuideCarriesTheRuntimesRulesForUnity() =>
            Assert.AreEqual(RqlLanguageGuide.RulesWith(RqlGuide.RuleExamples), RqlGuide.LanguageRules);

        // A receipt cut to its first rows does not point to a json format the tools do not have.
        [Test]
        public void ACutReceiptEndsWithTheToolsNote()
        {
            var text = Text(CallThroughMcpPlugin(Tool_Rql.MutateToolId,
                "{\"query\": \"range(0, 60) | call editor.CreateGameObject(\\\"Cut_\\\" + $)\", \"max_targets\": 100}").Value!);
            StringAssert.Contains("… 10 more row(s) not shown " + Tool_Rql.ReceiptMoreRowsNote, text);
            StringAssert.DoesNotContain("json format", text);
        }
    }
}
