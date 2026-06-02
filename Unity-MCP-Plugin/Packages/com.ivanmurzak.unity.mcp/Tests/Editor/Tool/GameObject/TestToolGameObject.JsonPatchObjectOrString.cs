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
using System;
using System.Collections;
using com.IvanMurzak.Unity.MCP.Editor.API;
using AIGD;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode coverage for the jsonPatch merge-patch surface resolving
    /// <c>UnityEngine.Object</c> references supplied as <c>{"instanceID": ...}</c>
    /// nodes — see issue #791.
    ///
    /// Before the fix, <c>Reflector.TryPatch</c> descended structurally into an
    /// object-ref node (e.g. <c>{"sharedMaterial":{"instanceID":"..."}}</c>) and
    /// failed with <c>"Segment 'instanceID' not found on type 'Material'"</c>.
    /// The fix overrides <c>TreatJsonObjectAsAtomicValue(Type) => true</c> on the
    /// Unity object/GameObject reflection converters so ReflectorNet 5.3.0 routes
    /// those atomic nodes through <c>SetValue</c>, resolving them to the live object.
    ///
    /// Each behaviour is covered in BOTH the object form (jsonPatch supplied as a
    /// JSON object literal) and the string form (the same JSON passed as a string),
    /// matching the dual-shape contract the <c>[JsonStringOrObject]</c> attribute
    /// exposes on the tool parameters. At the C# tool boundary <c>jsonPatch</c> is
    /// always a string, so both forms drive the identical <c>Reflector.TryPatch</c>
    /// apply path that the bug lived on; the string form additionally exercises a
    /// patch whose object-ref is embedded verbatim as the agent would send it.
    ///
    /// This file is version-agnostic: the only Unity-6.5-vs-pre-6.5 differences
    /// (EntityId/GetEntityId vs int/GetInstanceID, and the instanceID JSON wire form
    /// — string on 6.5, number on pre-6.5) are isolated in the version-split helper
    /// partial <c>TestToolGameObject.JsonPatchObjectOrString.Helpers(.pre-Unity.6.5).cs</c>,
    /// so the 8 tests compile and run on Unity 2022.3 through 6.5+.
    /// </summary>
    public partial class TestToolGameObject : BaseTest
    {
        // Builds a SolarSystem fixture identical in shape to PathBasedToolTests so the
        // multi-field regression below exercises the same surface the AI agent hits.
        (GameObject go, SolarSystem solar, GameObject sun, GameObject earth) BuildSolarFixtureForPatch()
        {
            var sun = new GameObject("Sun");
            var earth = new GameObject("Earth");
            var go = new GameObject("Solar");
            var solar = go.AddComponent<SolarSystem>();
            solar.sun = sun;
            solar.globalOrbitSpeedMultiplier = 1f;
            solar.globalSizeMultiplier = 1f;
            solar.planets = new[]
            {
                new SolarSystem.PlanetData
                {
                    planet = earth,
                    orbitRadius = 10f,
                    orbitSpeed = 1f,
                    rotationSpeed = 1f,
                    orbitTilt = Vector3.zero
                }
            };
            return (go, solar, sun, earth);
        }

        // ─── (a) regression: multi-field component jsonPatch still descends per-field ───

        [UnityTest]
        public IEnumerator JsonPatch_ObjectForm_MultiField_ScalarComponentPatch_StillDescends()
        {
            var (go, solar, _, _) = BuildSolarFixtureForPatch();

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(go),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                jsonPatch: "{\"globalOrbitSpeedMultiplier\": 7.5, \"globalSizeMultiplier\": 3.25}");

            Assert.IsTrue(response.Success, $"Multi-field scalar jsonPatch should still succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(7.5f, solar.globalOrbitSpeedMultiplier);
            Assert.AreEqual(3.25f, solar.globalSizeMultiplier);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JsonPatch_StringForm_MultiField_ScalarComponentPatch_StillDescends()
        {
            var (go, solar, _, _) = BuildSolarFixtureForPatch();

            // String form: identical JSON, asserting the dual-shape param does not regress
            // the ordinary per-field descent for non-object-ref scalar fields.
            var json = "{\"globalOrbitSpeedMultiplier\": 11.0, \"globalSizeMultiplier\": 4.0}";

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(go),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                jsonPatch: json);

            Assert.IsTrue(response.Success, $"String-form multi-field jsonPatch should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(11.0f, solar.globalOrbitSpeedMultiplier);
            Assert.AreEqual(4.0f, solar.globalSizeMultiplier);
            yield return null;
        }

        // ─── (b) asset-ref: set Renderer.sharedMaterial by {instanceID} ────────────────

        [UnityTest]
        public IEnumerator JsonPatch_ObjectForm_AssetRef_SetsSharedMaterialByInstanceId()
        {
            var folder = "Assets/JsonPatchObjectRefTests";
            var assetPath = $"{folder}/PatchMat.mat";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "JsonPatchObjectRefTests");

            var material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var go = new GameObject("Renderer host");
            var renderer = go.AddComponent<MeshRenderer>();
            try
            {
                Assert.IsNull(renderer.sharedMaterial, "Precondition: renderer has no shared material yet.");

                var json = InstanceIdPatch("sharedMaterial", material);

                var response = new Tool_GameObject().ModifyComponent(
                    gameObjectRef: GoRef(go),
                    componentRef: CompRef(renderer),
                    jsonPatch: json);

                Assert.IsTrue(response.Success, $"Asset-ref jsonPatch should resolve and assign the Material. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
                Assert.IsNotNull(renderer.sharedMaterial, "sharedMaterial should have been assigned.");
                Assert.IsTrue(SameRef(material, renderer.sharedMaterial!),
                    "The assigned Material must be the one referenced by {instanceID}.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator JsonPatch_StringForm_AssetRef_SetsSharedMaterialByInstanceId()
        {
            var folder = "Assets/JsonPatchObjectRefTests";
            var assetPath = $"{folder}/PatchMatString.mat";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "JsonPatchObjectRefTests");

            var material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var go = new GameObject("Renderer host str");
            var renderer = go.AddComponent<MeshRenderer>();
            try
            {
                // String form: the agent passes the patch as a JSON string; same apply path.
                var json = InstanceIdPatch("sharedMaterial", material);

                var response = new Tool_GameObject().ModifyComponent(
                    gameObjectRef: GoRef(go),
                    componentRef: CompRef(renderer),
                    jsonPatch: json);

                Assert.IsTrue(response.Success, $"String-form asset-ref jsonPatch should resolve the Material. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
                Assert.IsNotNull(renderer.sharedMaterial, "sharedMaterial should have been assigned (string form).");
                Assert.IsTrue(SameRef(material, renderer.sharedMaterial!),
                    "The assigned Material must be the one referenced by {instanceID} (string form).");
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            yield return null;
        }

        // ─── (c) component-ref: set HingeJoint.connectedBody (Rigidbody) by {instanceID} ─

        [UnityTest]
        public IEnumerator JsonPatch_ObjectForm_ComponentRef_SetsConnectedBodyByInstanceId()
        {
            var bodyGo = new GameObject("ConnectedBody");
            var rigidbody = bodyGo.AddComponent<Rigidbody>();

            var jointGo = new GameObject("Joint host");
            var hinge = jointGo.AddComponent<HingeJoint>();
            Assert.IsNull(hinge.connectedBody, "Precondition: hinge joint has no connected body yet.");

            var json = InstanceIdPatch("connectedBody", rigidbody);

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(jointGo),
                componentRef: CompRef(hinge),
                jsonPatch: json);

            Assert.IsTrue(response.Success, $"Component-ref jsonPatch should resolve and assign the Rigidbody. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.IsNotNull(hinge.connectedBody, "connectedBody should have been assigned.");
            Assert.IsTrue(SameRef(rigidbody, hinge.connectedBody!),
                "The assigned Rigidbody must be the one referenced by {instanceID}.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator JsonPatch_StringForm_ComponentRef_SetsConnectedBodyByInstanceId()
        {
            var bodyGo = new GameObject("ConnectedBody str");
            var rigidbody = bodyGo.AddComponent<Rigidbody>();

            var jointGo = new GameObject("Joint host str");
            var hinge = jointGo.AddComponent<HingeJoint>();

            var json = InstanceIdPatch("connectedBody", rigidbody);

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(jointGo),
                componentRef: CompRef(hinge),
                jsonPatch: json);

            Assert.IsTrue(response.Success, $"String-form component-ref jsonPatch should resolve the Rigidbody. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.IsNotNull(hinge.connectedBody, "connectedBody should have been assigned (string form).");
            Assert.IsTrue(SameRef(rigidbody, hinge.connectedBody!),
                "The assigned Rigidbody must be the one referenced by {instanceID} (string form).");
            yield return null;
        }

        // ─── (d) gameobject-ref: set a GameObject-typed field by {instanceID} ──────────

        [UnityTest]
        public IEnumerator JsonPatch_ObjectForm_GameObjectRef_SetsGameObjectFieldByInstanceId()
        {
            var (go, solar, _, _) = BuildSolarFixtureForPatch();
            var newSun = new GameObject("ReplacementSun");
            Assert.IsFalse(SameRef(newSun, solar.sun),
                "Precondition: solar.sun starts as the original Sun, not the replacement.");

            var json = InstanceIdPatch("sun", newSun);

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(go),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                jsonPatch: json);

            Assert.IsTrue(response.Success, $"GameObject-ref jsonPatch should resolve and assign the GameObject. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.IsNotNull(solar.sun, "solar.sun should still be assigned after the patch.");
            Assert.IsTrue(SameRef(newSun, solar.sun),
                "solar.sun must now be the GameObject referenced by {instanceID}.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator JsonPatch_StringForm_GameObjectRef_SetsGameObjectFieldByInstanceId()
        {
            var (go, solar, _, _) = BuildSolarFixtureForPatch();
            var newSun = new GameObject("ReplacementSun str");

            var json = InstanceIdPatch("sun", newSun);

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: GoRef(go),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                jsonPatch: json);

            Assert.IsTrue(response.Success, $"String-form GameObject-ref jsonPatch should resolve the GameObject. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.IsTrue(SameRef(newSun, solar.sun),
                "solar.sun must now be the GameObject referenced by {instanceID} (string form).");
            yield return null;
        }
    }
}
