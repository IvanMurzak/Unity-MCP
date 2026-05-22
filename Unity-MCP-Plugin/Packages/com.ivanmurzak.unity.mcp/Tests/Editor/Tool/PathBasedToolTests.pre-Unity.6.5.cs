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
#if !UNITY_6000_5_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode coverage for the ReflectorNet 5.1.0 path-based read/modify API
    /// adoption across the MCP tools — see issue #691 / PR adopting <c>TryReadAt</c>,
    /// <c>TryModifyAt</c>, <c>TryPatch</c>, and <c>View</c>.
    ///
    /// pre-Unity.6.5 variant of <see cref="PathBasedToolTests"/>: this Unity build
    /// uses <c>GetInstanceID()</c> instead of the Unity 6.5+ <c>GetEntityId()</c>
    /// helper. Test bodies are otherwise identical so reviewers can correlate the
    /// two files line-for-line.
    /// </summary>
    public class PathBasedToolTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        Reflector Reflector =>
            UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

        (GameObject go, SolarSystem solar, GameObject sun, GameObject earth) BuildSolarFixture()
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

        [UnityTest]
        public IEnumerator ComponentGet_SinglePath_ReturnsOnlyTheRequestedField()
        {
            var (go, _, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().GetComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                paths: new List<string> { "globalOrbitSpeedMultiplier" });

            Assert.IsNotNull(response.View);
            Assert.AreEqual(PathReadHelper.PathReadAggregateTypeName, response.View!.typeName);
            Assert.IsNull(response.Fields);
            Assert.IsNull(response.Properties);

            var only = response.View.fields?.SingleOrDefault(f => f.name == "globalOrbitSpeedMultiplier");
            Assert.IsNotNull(only);
            Assert.AreEqual(1f, only!.GetValue<float>(Reflector));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentGet_MultiPath_ReturnsAllRequestedFields()
        {
            var (go, _, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().GetComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                paths: new List<string>
                {
                    "globalOrbitSpeedMultiplier",
                    "globalSizeMultiplier",
                    "planets/[0]/orbitRadius"
                });

            Assert.IsNotNull(response.View);
            Assert.AreEqual(3, response.View!.fields?.Count);
            var byName = response.View!.fields!.ToDictionary(f => f.name!, f => f);
            Assert.AreEqual(1f,  byName["globalOrbitSpeedMultiplier"].GetValue<float>(Reflector));
            Assert.AreEqual(1f,  byName["globalSizeMultiplier"].GetValue<float>(Reflector));
            Assert.AreEqual(10f, byName["planets/[0]/orbitRadius"].GetValue<float>(Reflector));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentGet_ViewQuery_NameRegex_KeepsOnlyMatchingBranches()
        {
            var (go, _, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().GetComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                viewQuery: new ViewQuery { NamePattern = "orbit.*" });

            Assert.IsNotNull(response.View, "View should be populated for a view-query read.");

            // The regex 'orbit.*' should keep at least one matching name in the tree.
            // SolarSystem has globalOrbitSpeedMultiplier at root, plus orbitRadius / orbitSpeed
            // / orbitTilt on PlanetData. Any of these contain 'orbit' (case-insensitive).
            var rx = new Regex("orbit", RegexOptions.IgnoreCase);
            Assert.IsTrue(ContainsNameMatching(response.View!, rx),
                $"View result should retain at least one field/property whose name contains 'orbit'.");
            yield return null;
        }

        static bool ContainsNameMatching(SerializedMember m, Regex rx)
        {
            if (m.name != null && rx.IsMatch(m.name)) return true;
            if (m.fields != null)
                foreach (var f in m.fields) if (ContainsNameMatching(f, rx)) return true;
            if (m.props != null)
                foreach (var p in m.props) if (ContainsNameMatching(p, rx)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator ComponentModify_SinglePath_AtomicallyUpdatesScalar()
        {
            var (go, solar, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                pathPatches: new List<PathPatch>
                {
                    new PathPatch
                    {
                        Path = "globalOrbitSpeedMultiplier",
                        Value = SerializedMember.FromValue<float>(Reflector, 5f)
                    }
                });

            Assert.IsTrue(response.Success, $"Modify should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(5f, solar.globalOrbitSpeedMultiplier);
            Assert.AreEqual(1f, solar.globalSizeMultiplier);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentModify_JsonPatch_UpdatesMultipleFieldsAtOnce()
        {
            var (go, solar, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                jsonPatch: "{\"globalOrbitSpeedMultiplier\": 7.5, \"globalSizeMultiplier\": 3.25}");

            Assert.IsTrue(response.Success, $"JSON-patch modify should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(7.5f, solar.globalOrbitSpeedMultiplier);
            Assert.AreEqual(3.25f, solar.globalSizeMultiplier);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentModify_PartialArrayElement_UpdatesOneFieldOnTargetElement()
        {
            var (go, solar, _, _) = BuildSolarFixture();
            Assert.AreEqual(10f, solar.planets[0].orbitRadius);
            var earthRef = solar.planets[0].planet;

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                pathPatches: new List<PathPatch>
                {
                    new PathPatch
                    {
                        Path = "planets/[0]/orbitRadius",
                        Value = SerializedMember.FromValue<float>(Reflector, 42f)
                    }
                });

            Assert.IsTrue(response.Success, $"Partial array-element modify should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(42f, solar.planets[0].orbitRadius);
            Assert.AreEqual(1f, solar.planets[0].orbitSpeed);
            Assert.IsTrue(solar.planets[0].planet == earthRef);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentModify_InvalidPath_ReportsStructuredError()
        {
            var (go, solar, _, _) = BuildSolarFixture();
            var before = solar.globalOrbitSpeedMultiplier;

            // Reflector surfaces the unknown-segment failure as a Unity LogError via the bound
            // logger. Tell Unity's test framework we expect that error so it does not fail the test.
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new Regex("thisFieldDoesNotExist", RegexOptions.IgnoreCase));

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                pathPatches: new List<PathPatch>
                {
                    new PathPatch
                    {
                        Path = "thisFieldDoesNotExist",
                        Value = SerializedMember.FromValue<float>(Reflector, 999f)
                    }
                });

            Assert.IsFalse(response.Success,
                "Modify should report failure when every path patch fails.");
            Assert.IsTrue(response.Logs != null && response.Logs.Length > 0,
                "Failure must surface diagnostic logs.");
            var combined = string.Join("\n", response.Logs!);
            StringAssert.Contains("thisFieldDoesNotExist", combined,
                "Diagnostic logs should name the failing path so the AI agent can correct itself.");
            Assert.AreEqual(before, solar.globalOrbitSpeedMultiplier,
                "No untouched field should mutate when the only patch fails.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObjectGetData_SinglePath_ReturnsFilteredAggregate()
        {
            var go = new GameObject("Probe") { tag = "Untagged" };
            var result = new Tool_Object().GetData(
                new ObjectRef(go),
                paths: new List<string> { "name" });

            Assert.IsNotNull(result);
            Assert.AreEqual(PathReadHelper.PathReadAggregateTypeName, result!.typeName);
            var nameField = result.fields?.SingleOrDefault(f => f.name == "name");
            Assert.IsNotNull(nameField);
            Assert.AreEqual("Probe", nameField!.GetValue<string>(Reflector));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObjectModify_PathPatch_UpdatesGameObjectName()
        {
            var go = new GameObject("OldName");
            var response = new Tool_Object().Modify(
                new ObjectRef(go),
                pathPatches: new List<PathPatch>
                {
                    new PathPatch
                    {
                        Path = "name",
                        Value = SerializedMember.FromValue<string>(Reflector, "NewName")
                    }
                });

            Assert.IsTrue(response.Success, $"Path-patch modify on UnityEngine.Object should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual("NewName", go.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectModify_PerGameObjectPathPatches_ParallelArrays()
        {
            var go1 = new GameObject("First");
            var go2 = new GameObject("Second");

            var refs = new GameObjectRefList { new GameObjectRef(go1.GetInstanceID()), new GameObjectRef(go2.GetInstanceID()) };
            var perGo = new List<List<PathPatch>?>
            {
                new List<PathPatch>
                {
                    new PathPatch { Path = "name", Value = SerializedMember.FromValue<string>(Reflector, "FirstRenamed") }
                },
                new List<PathPatch>
                {
                    new PathPatch { Path = "name", Value = SerializedMember.FromValue<string>(Reflector, "SecondRenamed") }
                }
            };

            var logs = new Tool_GameObject().Modify(
                gameObjectRefs: refs,
                pathPatchesPerGameObject: perGo);

            Assert.IsNotNull(logs);
            Assert.AreEqual("FirstRenamed", go1.name);
            Assert.AreEqual("SecondRenamed", go2.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AssetsGetData_ViewQuery_FiltersByMaxDepth()
        {
            var folder = "Assets/PathBasedToolTests";
            var assetPath = $"{folder}/Mat.mat";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "PathBasedToolTests");

            var material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            try
            {
                var result = new Tool_Assets().GetData(
                    new AssetObjectRef(material),
                    viewQuery: new ViewQuery { MaxDepth = 0 });

                Assert.IsNotNull(result);
                Assert.IsTrue(result.fields == null || result.fields.Count == 0);
                Assert.IsTrue(result.props == null || result.props.Count == 0);
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
        public IEnumerator ComponentGet_NoPathParams_StillReturnsLegacyFieldsList()
        {
            var (go, _, _, _) = BuildSolarFixture();

            // Pass deepSerialization: true so the legacy Serialize call walks into the SolarSystem
            // user-defined fields (sun, planets, globalOrbitSpeedMultiplier, globalSizeMultiplier).
            // With deepSerialization: false (default) primitive-only top-level may produce a null
            // 'fields' list — that is correct behaviour for the legacy serializer and not what
            // we are asserting here; this regression test is about the *path-based change* not
            // accidentally suppressing the legacy fields surface.
            var response = new Tool_GameObject().GetComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                deepSerialization: true);

            Assert.IsNull(response.View, "Legacy code path must NOT populate 'View'.");
            // Legacy code path produced *some* serialised output. Either Fields or Properties
            // (or both) must be non-null and contain at least one entry.
            var fieldCount = response.Fields?.Count ?? 0;
            var propCount = response.Properties?.Count ?? 0;
            Assert.IsTrue(fieldCount + propCount > 0,
                $"Expected the legacy code path to surface at least one serialised member. " +
                $"Fields={fieldCount} Properties={propCount}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentGet_PathsAndViewQueryTogether_ThrowsArgumentException()
        {
            var (go, _, _, _) = BuildSolarFixture();

            var ex = Assert.Throws<ArgumentException>(() =>
                new Tool_GameObject().GetComponent(
                    gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                    componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                    paths: new List<string> { "globalOrbitSpeedMultiplier" },
                    viewQuery: new ViewQuery { NamePattern = "orbit.*" }));

            Assert.IsNotNull(ex);
            StringAssert.Contains("mutually exclusive", ex!.Message);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentModify_PathPatches_WithNullElement_DoesNotThrow_AndLogsSkip()
        {
            var (go, solar, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                pathPatches: new List<PathPatch>
                {
                    null!,
                    new PathPatch
                    {
                        Path = "globalOrbitSpeedMultiplier",
                        Value = SerializedMember.FromValue<float>(Reflector, 11f)
                    }
                });

            Assert.IsTrue(response.Success,
                $"At least one of the patches succeeded; overall result should be Success. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(11f, solar.globalOrbitSpeedMultiplier);
            var combined = string.Join("\n", response.Logs ?? Array.Empty<string>());
            StringAssert.Contains("PathPatch[0]", combined);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentGet_PathsWithEmptyEntry_DoesNotSerializeWholeObject()
        {
            var (go, _, _, _) = BuildSolarFixture();

            var response = new Tool_GameObject().GetComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                paths: new List<string> { "", "globalOrbitSpeedMultiplier" });

            Assert.IsNotNull(response.View);
            Assert.AreEqual(2, response.View!.fields?.Count);
            var emptyEntry = response.View!.fields![0];
            Assert.AreEqual(PathReadHelper.EmptyPathTypeName, emptyEntry.typeName);
            var resolved = response.View!.fields![1];
            Assert.AreEqual("globalOrbitSpeedMultiplier", resolved.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentModify_AllThreeSurfaces_ApplyInDocumentedOrder()
        {
            var (go, solar, _, _) = BuildSolarFixture();

            var diff = SerializedMember.FromValue(
                    reflector: Reflector,
                    name: null,
                    type: typeof(SolarSystem),
                    value: new ComponentRef { TypeName = typeof(SolarSystem).FullName! })
                .AddField(SerializedMember.FromValue(
                    reflector: Reflector,
                    name: "globalSizeMultiplier",
                    type: typeof(float),
                    value: 99f));

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef { TypeName = typeof(SolarSystem).FullName! },
                componentDiff: diff,
                pathPatches: new List<PathPatch>
                {
                    new PathPatch
                    {
                        Path = "planets/[0]/orbitRadius",
                        Value = SerializedMember.FromValue<float>(Reflector, 77f)
                    }
                },
                jsonPatch: "{\"globalOrbitSpeedMultiplier\": 33.0}");

            Assert.IsTrue(response.Success, $"Combined modify should succeed. Logs: {string.Join(", ", response.Logs ?? Array.Empty<string>())}");
            Assert.AreEqual(33f, solar.globalOrbitSpeedMultiplier);
            Assert.AreEqual(77f, solar.planets[0].orbitRadius);
            Assert.AreEqual(99f, solar.globalSizeMultiplier);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameObjectModify_PathPatchesPerGameObject_LengthMismatch_ThrowsArgumentException()
        {
            var go1 = new GameObject("First");
            var go2 = new GameObject("Second");
            var refs = new GameObjectRefList { new GameObjectRef(go1.GetInstanceID()), new GameObjectRef(go2.GetInstanceID()) };

            var perGo = new List<List<PathPatch>?>
            {
                new List<PathPatch>
                {
                    new PathPatch { Path = "name", Value = SerializedMember.FromValue<string>(Reflector, "Solo") }
                }
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                new Tool_GameObject().Modify(gameObjectRefs: refs, pathPatchesPerGameObject: perGo));
            Assert.IsNotNull(ex);
            Assert.AreEqual("pathPatchesPerGameObject", ex!.ParamName);

            var jsonPatches = new List<string?> { "{\"name\":\"Solo\"}" };
            var ex2 = Assert.Throws<ArgumentException>(() =>
                new Tool_GameObject().Modify(gameObjectRefs: refs, jsonPatchesPerGameObject: jsonPatches));
            Assert.IsNotNull(ex2);
            Assert.AreEqual("jsonPatchesPerGameObject", ex2!.ParamName);
            yield return null;
        }
    }
}
#endif
