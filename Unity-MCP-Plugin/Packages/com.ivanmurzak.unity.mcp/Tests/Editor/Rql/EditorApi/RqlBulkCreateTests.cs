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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>
    /// Bulk create (RQL spec §8 source-qualified call): <c>range(…) | call editor.CreateGameObject(…)</c> runs once per item,
    /// checks every item before the first call, and undoes as one step.
    /// </summary>
    public class RqlBulkCreateTests : EditorApiTestBase
    {
        private static GameObject[] Roots() => SceneManager.GetActiveScene().GetRootGameObjects();

        [Test]
        public void RangeCreatesAGridInOneUndoStep()
        {
            Undo.ClearAll();
            var r = M("range(0, 30) | call editor.CreateGameObject(\"Cube_\" + $, {primitive: \"Cube\", position: [($ % 5) * 2, 0, floor($ / 5) * 2]})");
            AssertOk(r);
            Assert.AreEqual(30, r.Rows!.Count, Table(r));
            var cubes = Roots().Where(g => g.name.StartsWith("Cube_")).ToList();
            Assert.AreEqual(30, cubes.Count);
            Assert.IsTrue(cubes.All(c => c.GetComponent<MeshFilter>() != null));
            Assert.AreEqual(new Vector3(6, 0, 4), cubes.Single(c => c.name == "Cube_13").transform.localPosition);

            // Every receipt row carries the created object's $ref, not the editor's.
            var refs = Table(r);
            foreach (var cube in cubes) StringAssert.Contains(RqlUnity.Engine.RefOf(cube), refs);

            Undo.PerformUndo();
            Assert.IsFalse(Roots().Any(g => g.name.StartsWith("Cube_")), "one Undo step removes all 30");
        }

        [Test]
        public void ListLiteralItemsAndPlacementMembers()
        {
            var parent = new GameObject("Shelf");
            var parentRef = RqlUnity.Engine.RefOf(parent);
            AssertOk(M($"[[\"Crate\", [1, 0, 0]], [\"Barrel\", [3, 0, 0]]] | call editor.CreateGameObject($[0], " +
                       $"{{parent: ref(\"{parentRef}\"), position: $[1], rotation: [0, 90, 0], scale: [2, 2, 2]}})"));
            var barrel = parent.transform.Find("Barrel");
            Assert.IsNotNull(barrel);
            Assert.AreEqual(new Vector3(3, 0, 0), barrel.localPosition);
            Assert.AreEqual(90f, barrel.localEulerAngles.y, 1e-3f);
            Assert.AreEqual(new Vector3(2, 2, 2), barrel.localScale);
            Assert.IsNotNull(parent.transform.Find("Crate"));
        }

        [Test]
        public void EveryItemIsCheckedBeforeTheFirstCall()
        {
            AssertFails(M("[\"First\", \"\"] | call editor.CreateGameObject($)"), "invalid-value", "cannot be empty");
            AssertFails(M("range(0, 3) | call editor.CreateGameObject(\"P\" + $, {primitive: ($ == 2 ? \"Pyramid\" : \"Cube\")})"),
                "invalid-value", "unknown primitive 'Pyramid'");
            Assert.IsFalse(Roots().Any(g => g.name == "First" || g.name.StartsWith("P")), "nothing was created");
        }

        [Test]
        public void DryRunCreatesNothing()
        {
            var r = M("range(0, 4) | call editor.CreateGameObject(\"Dry\" + $, {primitive: \"Sphere\"})", dryRun: true);
            AssertOk(r);
            Assert.AreEqual(4, r.Rows!.Count, Table(r));
            Assert.IsFalse(Roots().Any(g => g.name.StartsWith("Dry")));
        }

        [Test]
        public void TheTargetLimitCountsItems()
        {
            AssertFails(M("range(0, 201) | call editor.CreateGameObject(\"X\" + $)"), "mutation-limit", "max_targets");
            Assert.IsFalse(Roots().Any(g => g.name.StartsWith("X")));
        }

        [Test]
        public void InstantiatePrefabPerItem()
        {
            var source = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var path = Folder + "/Coin.prefab";
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);

            AssertOk(M($"range(0, 3) | call editor.InstantiatePrefab(\"{path}\", {{position: [$ * 2, 1, 0]}})"));
            var coins = Roots().Where(g => g.name == "Coin").ToList();
            Assert.AreEqual(3, coins.Count);
            Assert.IsTrue(coins.All(PrefabUtility.IsPartOfPrefabInstance));
            Assert.IsTrue(coins.Any(c => c.transform.localPosition == new Vector3(4, 1, 0)));

            AssertFails(M($"range(0, 2) | call editor.InstantiatePrefab(\"{Folder}/Missing.prefab\", {{position: [0, 0, 0]}})"),
                "invalid-value", "no prefab at");
        }

        // An object literal never becomes a Unity object: RQL would construct the parameter's type, and new GameObject() puts a
        // stray object in the scene.
        [Test]
        public void AnObjectLiteralIsNotAGameObject()
        {
            AssertFails(M("editor | call CreateGameObject(\"Child\", {}, \"Cube\")"), "coercion-failed", "an object literal cannot become a GameObject");
            Assert.AreEqual(0, Roots().Length, string.Join(", ", Roots().Select(g => g.name)));
        }

        [Test]
        public void TheGuideTeachesBulkCreateAndCarriesTheSharedRules()
        {
            StringAssert.Contains("range(0, 300) | call editor.CreateGameObject(", RqlGuide.MutateDescription);
            StringAssert.Contains("transform.localScale * 1.5", RqlGuide.MutateDescription);
            StringAssert.Contains(RqlLanguageGuide.Rules, RqlGuide.QueryDescription);
            // The shared rules replace the guide's own copy of them.
            Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(RqlGuide.Language, "Lists compare by value").Count);
        }
    }
}
