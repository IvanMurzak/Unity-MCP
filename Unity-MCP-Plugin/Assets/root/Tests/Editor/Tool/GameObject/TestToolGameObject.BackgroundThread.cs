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
using System.Collections;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        // ── background thread tests ───────────────────────────────────────

        [UnityTest]
        public IEnumerator Find_FromBackgroundThread_ByInstanceID_Succeeds()
        {
            var go = new GameObject("BGFind");

            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-find", $@"{{
                    ""gameObjectRef"": {{
                        ""instanceID"": {go.GetInstanceID()}
                    }}
                }}"));
        }

        [UnityTest]
        public IEnumerator Create_FromBackgroundThread_Succeeds()
        {
            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-create", @"{
                    ""name"": ""BGCreatedObject"",
                    ""position"": { ""x"": 0, ""y"": 0, ""z"": 0 }
                }"));
        }

        [UnityTest]
        public IEnumerator ComponentListAll_FromBackgroundThread_Succeeds()
        {
            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-component-list-all", "{}"));
        }

        [UnityTest]
        public IEnumerator Destroy_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject("BGDestroyObject");
            var instanceId = go.GetInstanceID();

            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-destroy", $@"{{
                    ""gameObjectRefs"": [{{
                        ""instanceID"": {instanceId}
                    }}]
                }}"));

            // Verify destroyed (must check on main thread)
            var found = UnityEngine.GameObject.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNull(System.Array.Find(found, x => x.GetInstanceID() == instanceId),
                "GameObject should be destroyed");
        }

        [UnityTest]
        public IEnumerator Modify_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject("BGModifyObject");

            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-modify", $@"{{
                    ""gameObjectRefs"": [{{
                        ""instanceID"": {go.GetInstanceID()}
                    }}],
                    ""name"": ""BGModifiedObject""
                }}"));

            Assert.AreEqual("BGModifiedObject", go.name,
                "GameObject name should be modified from background thread");
        }

        [UnityTest]
        public IEnumerator ComponentGet_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject("BGComponentGetObject");
            go.AddComponent<BoxCollider>();

            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-component-get", $@"{{
                    ""gameObjectRef"": {{
                        ""instanceID"": {go.GetInstanceID()}
                    }},
                    ""componentTypeName"": ""UnityEngine.BoxCollider""
                }}"));
        }

        [UnityTest]
        public IEnumerator Duplicate_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject("BGDuplicateSource");

            yield return RunOnBackgroundThread(() =>
                RunTool("gameobject-duplicate", $@"{{
                    ""gameObjectRefs"": [{{
                        ""instanceID"": {go.GetInstanceID()}
                    }}]
                }}"));
        }
    }
}
