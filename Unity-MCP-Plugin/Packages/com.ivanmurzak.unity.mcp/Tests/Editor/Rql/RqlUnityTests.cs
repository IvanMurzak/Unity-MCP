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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.ReflectorNet;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql
{
    public class RqlUnityTests : Adapters.AdapterTestBase
    {
        private GameObject _a = null!;
        private GameObject _b = null!;
        private Light _light = null!;

        [SetUp]
        public void SetUp()
        {
            _a = new GameObject("Alpha");
            _a.transform.position = new Vector3(1, 2, 3);
            _b = new GameObject("Beta");
            _b.transform.SetParent(_a.transform, false);
            _light = new GameObject("Lamp").AddComponent<Light>();
            _light.intensity = 2.5f;
            _light.color = new Color(1, 0, 0, 1);
        }

        [Test]
        public void QueriesGameObjectsAndTransformMembers()
        {
            Assert.AreEqual("(1, 2, 3)", Cell(Q("objects | where name == \"Alpha\" | select transform.position")));
            var count = Q("objects | count");
            Assert.IsTrue(count.Ok, Table(count));
            Assert.AreEqual(3L, count.Value!.GetValue<long>());
            Assert.AreEqual("Beta", Cell(Q("objects | where name == \"Alpha\" | get children | select name")));
            Assert.AreEqual("Alpha", Cell(Q("objects | where name == \"Beta\" | select transform.parent.gameObject.name")));
        }

        [Test]
        public void FindsComponentsByType()
        {
            var r = Q("objects | get components | where $ is Light | select gameObject.name intensity color");
            Assert.AreEqual("Lamp", Cell(r, 0, 0));
            Assert.AreEqual("2.5", Cell(r, 0, 1));
            Assert.AreEqual("(1, 0, 0, 1)", Cell(r, 0, 2));
        }

        [Test]
        public void VectorsAcceptStringListAndObjectLiterals()
        {
            Assert.IsTrue(M("objects | where name == \"Alpha\" | set transform.position = \"(4, 5, 6)\"").Ok);
            Assert.AreEqual(new Vector3(4, 5, 6), _a.transform.position);
            Assert.IsTrue(M("objects | where name == \"Alpha\" | set transform.position = [7, 8, 9]").Ok);
            Assert.AreEqual(new Vector3(7, 8, 9), _a.transform.position);
            var obj = M("objects | where name == \"Alpha\" | set transform.position = {x: 1, y: 1, z: 1}");
            Assert.IsTrue(obj.Ok, Table(obj));
            Assert.AreEqual(Vector3.one, _a.transform.position);
            Assert.IsTrue(M("objects | get components | where $ is Light | set color = \"#00ff00\"").Ok);
            Assert.AreEqual(Color.green, _light.color);
            Assert.AreEqual("Alpha", Cell(Q("objects | where transform.position == \"(1, 1, 1)\" | select name")));
        }

        [Test]
        public void MutationsAreUndoableAndAtomic()
        {
            var dry = M("objects | where name == \"Alpha\" | set name = \"Renamed\"", dryRun: true);
            Assert.IsTrue(dry.Ok, Table(dry));
            Assert.AreEqual("Alpha", _a.name);

            Assert.IsTrue(M("objects | where name == \"Alpha\" | set name = \"Renamed\", active = false").Ok);
            Assert.AreEqual("Renamed", _a.name);
            Assert.IsFalse(_a.activeSelf);
            Undo.PerformUndo();
            Assert.AreEqual("Alpha", _a.name);
            Assert.IsTrue(_a.activeSelf);

            // An invalid value anywhere means nothing is written.
            var bad = M("objects | where name == \"Alpha\" | set name = \"X\", transform.position = \"nope\"");
            Assert.IsFalse(bad.Ok);
            Assert.AreEqual("coercion-failed", bad.Diagnostics[0].Code);
            Assert.AreEqual("Alpha", _a.name);
        }

        [Test]
        public void DestroyedObjectsReadAsNullAndRefsGoStale()
        {
            var id = Cell(Q("objects | where name == \"Beta\" | select $ref"));
            StringAssert.StartsWith("GameObject#", id);
            Assert.AreEqual("Beta", Cell(Q($"ref(\"{id}\") | select name")));
            Object.DestroyImmediate(_b);
            var stale = Q($"ref(\"{id}\") | select name");
            Assert.IsFalse(stale.Ok);
            Assert.AreEqual("stale-ref", stale.Diagnostics[0].Code);
        }

        [Test]
        public void MaterialShaderPropertiesAreAMap()
        {
            var shader = Shader.Find("Standard");
            Assume.That(shader != null, "Standard shader not available in this render pipeline");
            var material = new Material(shader) { name = "TestMat", color = Color.blue };
            var renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            Assert.AreEqual("(0, 0, 1, 1)", Cell(Q("objects | get components | where $ is MeshRenderer | select c=sharedMaterial.properties[\"_Color\"]")));
            var set = M("objects | get components | where $ is MeshRenderer | set sharedMaterial.properties[\"_Glossiness\"] = 0.25");
            Assert.IsTrue(set.Ok, Table(set));
            Assert.AreEqual(0.25f, material.GetFloat("_Glossiness"), 1e-6f);
            var outOfRange = M("objects | get components | where $ is MeshRenderer | set sharedMaterial.properties[\"_Glossiness\"] = 3");
            Assert.IsFalse(outOfRange.Ok, Table(outOfRange));
        }

        [Test]
        public void ChangesOnlyTheAlphaOfAMaterialColorPropertyFoundByInstanceId()
        {
            var shader = Shader.Find("Standard");
            Assume.That(shader != null, "Standard shader not available in this render pipeline");
            var material = new Material(shader) { name = "Glow" };
            material.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.6f, 1f));
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var id = Cell(Q("objects | where $ is GameObject and components.any($ is MeshRenderer) | select $ref"));

            var before = Q($"ref(\"{id}\") | get components | where $ is Renderer | select sharedMaterial.name c=sharedMaterial.properties[\"_EmissionColor\"]");
            AssertTuple("(0.2, 0.4, 0.6, 1)", Cell(before, 0, 1));

            var set = M($"ref(\"{id}\") | get components | where $ is Renderer | set sharedMaterial.properties[\"_EmissionColor\"].a = 0.25");
            Assert.IsTrue(set.Ok, Table(set));
            var c = material.GetColor("_EmissionColor");
            Assert.AreEqual(0.25f, c.a, 1e-6f);
            Assert.AreEqual(0.2f, c.r, 1e-6f);
            Assert.AreEqual(0.4f, c.g, 1e-6f);
            Assert.AreEqual(0.6f, c.b, 1e-6f);
            UnityEngine.Debug.Log("[RQL] alpha receipt:\n" + Table(set));
        }

        [Test]
        public void CreatesObjectsAddsComponentsReparentsAndDestroys()
        {
            var alphaRef = Cell(Q("objects | where name == \"Alpha\" | select $ref"));
            var created = M($"editor | call CreateGameObject(\"Crate\", ref(\"{alphaRef}\"), \"Cube\")");
            Assert.IsTrue(created.Ok, Table(created));
            var crate = GameObject.Find("Crate");
            Assert.IsNotNull(crate);
            Assert.AreEqual(_a.transform, crate.transform.parent);
            Assert.IsNotNull(crate.GetComponent<MeshRenderer>());

            var add = M("objects | where name == \"Crate\" | call AddComponent(\"Rigidbody\")");
            Assert.IsTrue(add.Ok, Table(add));
            Assert.IsTrue(M("objects | get components | where $ is Rigidbody | set mass = 50").Ok);
            Assert.AreEqual(50f, crate.GetComponent<Rigidbody>().mass, 1e-6f);

            var betaRef = Cell(Q("objects | where name == \"Beta\" | select $ref"));
            var reparent = M($"objects | where name == \"Crate\" | set transform.parent = ref(\"{betaRef}\")");
            Assert.IsTrue(reparent.Ok, Table(reparent));
            Assert.AreEqual(_b.transform, crate.transform.parent);

            Assert.IsTrue(M("objects | where name == \"Crate\" | get components | where $ is BoxCollider | call Destroy()").Ok);
            Assert.IsNull(crate.GetComponent<BoxCollider>());
            Assert.IsTrue(M("objects | where name == \"Crate\" | call Destroy()").Ok);
            Assert.IsTrue(crate == null);
            Undo.PerformUndo();
            Assert.IsNotNull(GameObject.Find("Crate"), "destroy is undoable");
            Assert.AreEqual("unknown-function", M("objects | where name == \"Alpha\" | call AddComponent(\"NoSuchComponent\")").Diagnostics.Count == 0
                ? "?" : "unknown-function");
        }

        [Test]
        public void SchemaListsSceneTypesAndOneType()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("objects", overview);
            StringAssert.Contains("Light ×1", overview);
            StringAssert.Contains("GameObject", overview);
            var light = RqlUnitySchema.Render(RqlUnity.Engine, "Light");
            StringAssert.Contains("intensity", light);
            StringAssert.Contains("unknown-type", RqlUnitySchema.Render(RqlUnity.Engine, "NoSuchTypeAtAll"));
        }

        [Test]
        public void ToolsAreRegisteredAndCallableThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var names = UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Select(t => t.Name).ToList();
            CollectionAssert.IsSubsetOf(new[] { Tool_Rql.QueryToolId, Tool_Rql.MutateToolId, Tool_Rql.SchemaToolId }, names);

            var query = CallThroughMcpPlugin(Tool_Rql.QueryToolId, "{\"query\": \"objects | where name == \\\"Alpha\\\" | select name transform.position\"}");
            var json = query.ToJson(UnityMcpPluginEditor.Instance.Reflector)!;
            Assert.AreEqual(ResponseStatus.Success, query.Value!.Status, json);
            StringAssert.Contains("name|transform.position", json);   // the compact table travels as text content
            StringAssert.Contains("(1, 2, 3)", json);

            var dry = CallThroughMcpPlugin(Tool_Rql.MutateToolId, "{\"query\": \"objects | where name == \\\"Alpha\\\" | set name = \\\"Z\\\"\", \"dry_run\": true}");
            Assert.AreEqual(ResponseStatus.Success, dry.Value!.Status, dry.ToJson(UnityMcpPluginEditor.Instance.Reflector));
            Assert.AreEqual("Alpha", _a.name);

            var schema = CallThroughMcpPlugin(Tool_Rql.SchemaToolId, "{}");                 // optional argument omitted
            Assert.AreEqual(ResponseStatus.Success, schema.Value!.Status, schema.ToJson(UnityMcpPluginEditor.Instance.Reflector));
        }

        // $index in a set value is the target's position in the stream entering set; text functions as methods;
        // group-by with two keys gives one column per key.
        [Test]
        public void SpreadsWithIndexRenamesWithReplaceAndGroupsByTwoKeys()
        {
            var c = new GameObject("Crate_C");
            var a = new GameObject("Crate_A");
            var b = new GameObject("Crate_B");
            AssertOk(M("objects | where name starts-with \"Crate_\" | sort name | set transform.position.x = $index * 2"));
            Assert.AreEqual(0f, a.transform.position.x);
            Assert.AreEqual(2f, b.transform.position.x);
            Assert.AreEqual(4f, c.transform.position.x);

            AssertOk(M("objects | where name starts-with \"Crate_\" | set name = name.replace(\"Crate_\", \"Box_\")"));
            CollectionAssert.AreEquivalent(new[] { "Box_A", "Box_B", "Box_C" }, new[] { a.name, b.name, c.name });

            var groups = Q("objects | where name starts-with \"Box_\" | group-by $type, far=(transform.position.x > 1)");
            Assert.IsTrue(groups.Ok, Table(groups));
            CollectionAssert.AreEqual(new[] { "$type", "far", "count" }, groups.Columns);
            Assert.AreEqual(2, groups.Rows!.Count, Table(groups));
        }

        // The host's hint: ref() given a name points at unity.find (the mutation-limit hint is MaxTargetsRaisesTheMutationLimit's).
        [Test]
        public void RefHintNamesTheFinder()
        {
            var byName = Q("ref(\"Alpha\") | select name");
            Assert.IsFalse(byName.Ok);
            StringAssert.Contains("unity.find(", Table(byName));
        }

        // Hints name what this host has: the schema tool, a real source, and the source that has an unknown call method.
        [Test]
        public void HintsNameTheHostsToolsAndSources()
        {
            var noSource = Q("[0, 1, 2] | select $");
            AssertFails(noSource, "unknown-source", "(e.g. objects)");
            StringAssert.DoesNotContain("e.g. world", Table(noSource)); // (an extension may add tilemap.worldToCell)
            AssertFails(M("objects | where name == \"Alpha\" | call CreateGameObject(\"Made\")"), "unknown-function",
                "editor | call CreateGameObject");
        }

        // Members of any Unity type count as known (internal scripts and adapter members too): a polymorphic set that no item
        // reaches stays a no-op; a name no type has is a typo that fails.
        [Test]
        public void KnownMembersCoverEveryUnityType()
        {
            Assert.IsTrue(UnityTypes.IsKnownMember(RqlUnity.Engine, "orthographicSize"));
            Assert.IsTrue(UnityTypes.IsKnownMember(RqlUnity.Engine, "rqlInternalOnlyMember"), "internal MonoBehaviour field");
            Assert.IsTrue(UnityTypes.IsKnownMember(RqlUnity.Engine, "prefabSource"), "adapter member");
            Assert.IsFalse(UnityTypes.IsKnownMember(RqlUnity.Engine, "transfrom"));
            AssertOk(M("objects | get components | where rqlInternalOnlyMember > 1 | set rqlInternalOnlyMember = 2"));
            AssertFails(M("objects | get components | where rqlInternalOnlyMembr > 1 | set rqlInternalOnlyMembr = 2"), "unknown-member");
        }

        // c ? a : b and if(c, a, b); only the taken branch runs.
        [Test]
        public void ConditionalsPickABranch()
        {
            var r = Q("objects | where name in [\"Alpha\", \"Lamp\"] | get components | where not ($ is Transform) | select k = ($ is Light ? \"light\" : \"other\")");
            Assert.AreEqual("light", Cell(r), Table(r));
            Assert.AreEqual("on", Cell(Q("objects | where name == \"Alpha\" | select k = if(active, \"on\", \"off\")")));
        }

        // Enum values also take the Inspector's names (nicified or [InspectorName]).
        [Test]
        public void EnumsAcceptInspectorNames()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            AssertOk(M("objects | where name == \"Cube\" | get components | where $ is MeshRenderer | set shadowCastingMode = \"Two Sided\""));
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.TwoSided, cube.GetComponent<MeshRenderer>().shadowCastingMode);
            Assert.AreEqual("Cube", Cell(Q("objects | get components | where $ is MeshRenderer and shadowCastingMode == \"Two Sided\" | select gameObject.name")));
            var aliases = UnityTypes.EnumAliases(typeof(UnityEngine.Rendering.ShadowCastingMode))!;
            Assert.AreEqual("ShadowsOnly", aliases["Shadows Only"]);
            Assert.IsFalse(aliases.ContainsKey("On"), "an entry's own name is never an alias");
        }

        // Conditions must be booleans (RQL spec §1.2): an object in a where fails with a hint showing the test to write.
        [Test]
        public void ConditionsMustBeBoolean()
        {
            GameObject.CreatePrimitive(PrimitiveType.Cube).name = "Cube";
            const string Renderers = "objects | get components | where $ is MeshRenderer";
            AssertFails(Q(Renderers + " | where sharedMaterial | select gameObject.name"), "type-mismatch", "sharedMaterial != null");
            Assert.AreEqual("Cube", Cell(Q(Renderers + " | where sharedMaterial != null | select gameObject.name")));
            Assert.AreEqual("Cube", Cell(Q(Renderers + " | where has(sharedMaterial) | select gameObject.name")));
            AssertFails(Q("objects | where name == \"Alpha\" and children.count() | select name"), "type-mismatch");
        }

        // Group rows carry their items; float members aggregate as their shortest decimal (2.5 and 2.7 average to 2.6).
        [Test]
        public void GroupItemsAggregateFloatMembers()
        {
            new GameObject("Lamp2").AddComponent<Light>().intensity = 2.7f;
            var r = Q("objects | get components | where $ is Light | group-by type | select key, count, avg = items.avg(intensity)");
            Assert.AreEqual("Point", Cell(r, 0, 0), Table(r));
            Assert.AreEqual("2", Cell(r, 0, 1));
            Assert.AreEqual("2.6", Cell(r, 0, 2));
            Assert.AreEqual("2.6", Q("objects | get components | where $ is Light | avg intensity").Value!.ToJsonString());
        }

        // Lists compare by value: equal lists are equal and merge in group-by.
        [Test]
        public void ListsCompareByValue()
        {
            Assert.AreEqual("1", Cell(Q("objects | where name == \"Alpha\" | select ok = ([1, 2] == [1, 2] ? 1 : 0)")));
            Assert.AreEqual(1L, Count("objects | where name in [\"Alpha\", \"Beta\"] | group-by [1, 2] | count"));
        }

        // A set from rql-query: the permission-denied hint names rql-mutate.
        [Test]
        public void PermissionDeniedNamesTheMutateTool()
        {
            AssertFails(Q("objects | where name == \"Alpha\" | set name = \"X\""), "permission-denied", Tool_Rql.MutateToolId);
            Assert.AreEqual("Alpha", _a.name);
        }

        // Receipts leave out assignments that keep the old value and count them as unchanged (the writes still happen).
        [Test]
        public void NoOpAssignmentsAreCountedAsUnchanged()
        {
            var r = RqlUnity.Mutate("objects | where name in [\"Alpha\", \"Beta\", \"Lamp\"] | set name = (name == \"Beta\" ? \"Gamma\" : name)");
            AssertOk(r);
            Assert.AreEqual(1, r.Rows!.Count, Table(r));
            Assert.AreEqual(2L, r.Unchanged, Table(r));
            StringAssert.Contains("2 unchanged", Table(r));
            Assert.AreEqual("Gamma", _b.name);
        }

        [Test]
        public void ToolsReturnTextResults()
        {
            var tool = new Tool_Rql();
            var ok = tool.Query("objects | where name == \"Alpha\" | select name");
            Assert.AreEqual(ResponseStatus.Success, ok.Status);
            var failed = tool.Query("objects | where (");
            Assert.AreEqual(ResponseStatus.Error, failed.Status);
            var schema = tool.Schema("Transform");
            Assert.AreEqual(ResponseStatus.Success, schema.Status);
        }
    }

    // Only for KnownMembersCoverEveryUnityType: an internal script type whose member no other type declares.
    internal sealed class RqlInternalOnlyBehaviour : MonoBehaviour
    {
        public int rqlInternalOnlyMember;
    }
}
