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
#if UNITY_6000_5_OR_NEWER
using System.Collections;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class AssetsPrefabCreateTests : BaseTest
    {
        const string TestFolderName = "Unity-MCP-Test-PrefabCreate";
        const string TestFolder = "Assets/" + TestFolderName;

        [TearDown]
        public void PrefabTearDown()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
        }

        void EnsureTestFolder()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.CreateFolder("Assets", TestFolderName);
        }

        // ================================================================
        // Main thread — create from scene GameObject
        // ================================================================

        [UnityTest]
        public IEnumerator Prefab_Create_Connected_MainThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Connected_Main.prefab";
            var go = new GameObject("TestGO_Connected_Main");
            var id = go.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}},""connectGameObjectToPrefab"":true}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(go),
                "Scene GameObject should be connected to the prefab (prefab instance).");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_NotConnected_MainThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Disconnected_Main.prefab";
            var go = new GameObject("TestGO_Disconnected_Main");
            var id = go.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}},""connectGameObjectToPrefab"":false}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(go),
                "Scene GameObject should NOT be connected to the prefab.");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_DefaultConnected_MainThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Default_Main.prefab";
            var go = new GameObject("TestGO_Default_Main");
            var id = go.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}}}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(go),
                "Default should connect scene GameObject to the prefab.");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_AutoCreatesFolders_MainThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Nested/Deep/Folder/AutoCreated.prefab";
            var go = new GameObject("TestGO_Nested_Main");
            var id = go.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}}}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at nested path: {prefabPath}");
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{TestFolder}/Nested/Deep/Folder"),
                "Nested folders should have been created.");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_WithComponents_MainThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Components_Main.prefab";
            var go = new GameObject("TestGO_Components_Main");
            go.AddComponent<BoxCollider>();
            go.AddComponent<Rigidbody>();
            var id = go.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}},""connectGameObjectToPrefab"":false}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsNotNull(prefab!.GetComponent<BoxCollider>(), "Prefab should have BoxCollider.");
            Assert.IsNotNull(prefab.GetComponent<Rigidbody>(), "Prefab should have Rigidbody.");
        }

        // ================================================================
        // Main thread — Prefab Variant from scene prefab instance
        // ================================================================

        [UnityTest]
        public IEnumerator Prefab_CreateVariant_FromScenePrefabInstance_MainThread()
        {
            EnsureTestFolder();

            // Step 1: create a base prefab
            var basePath = $"{TestFolder}/Base_Variant.prefab";
            var baseGo = new GameObject("TestGO_Base_Variant");
            baseGo.AddComponent<BoxCollider>();
            var baseId = baseGo.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{basePath}"",""gameObjectRef"":{{""instanceID"":{baseId}}},""connectGameObjectToPrefab"":true}}");

            // baseGo is now a prefab instance — modify it and save as variant
            baseGo.AddComponent<SphereCollider>();
            var variantPath = $"{TestFolder}/Variant_FromInstance.prefab";

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{variantPath}"",""gameObjectRef"":{{""instanceID"":{baseId}}},""connectGameObjectToPrefab"":true}}");

            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.IsNotNull(variant, $"Variant prefab should be created at: {variantPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(variant!),
                "Created prefab should be a Prefab Variant.");
        }

        // ================================================================
        // Main thread — Prefab Variant from asset path
        // ================================================================

        [UnityTest]
        public IEnumerator Prefab_CreateVariant_FromAssetPath_MainThread()
        {
            EnsureTestFolder();

            // Step 1: create a base prefab
            var basePath = $"{TestFolder}/Base_AssetVariant.prefab";
            var baseGo = new GameObject("TestGO_Base_AssetVariant");
            baseGo.AddComponent<BoxCollider>();
            var baseId = baseGo.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{basePath}"",""gameObjectRef"":{{""instanceID"":{baseId}}},""connectGameObjectToPrefab"":false}}");

            // Step 2: create a variant from the asset path
            var variantPath = $"{TestFolder}/Variant_FromAsset.prefab";
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{variantPath}"",""sourcePrefabAssetPath"":""{basePath}""}}");

            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.IsNotNull(variant, $"Variant prefab should be created at: {variantPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(variant!),
                "Created prefab from asset path should be a Prefab Variant.");
            Assert.IsNotNull(variant!.GetComponent<BoxCollider>(),
                "Variant should inherit BoxCollider from the base prefab.");
        }

        // ================================================================
        // Background thread tests
        // ================================================================

        [UnityTest]
        public IEnumerator Prefab_Create_Connected_BackgroundThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Connected_Bg.prefab";
            var go = new GameObject("TestGO_Connected_Bg");
            var id = go.GetEntityId();

            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}},""connectGameObjectToPrefab"":true}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(go),
                "Scene GameObject should be connected to the prefab (background thread).");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_NotConnected_BackgroundThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/Disconnected_Bg.prefab";
            var go = new GameObject("TestGO_Disconnected_Bg");
            var id = go.GetEntityId();

            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}},""connectGameObjectToPrefab"":false}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at: {prefabPath}");
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(go),
                "Scene GameObject should NOT be connected to the prefab (background thread).");
        }

        [UnityTest]
        public IEnumerator Prefab_Create_AutoCreatesFolders_BackgroundThread()
        {
            EnsureTestFolder();
            var prefabPath = $"{TestFolder}/NestedBg/DeepBg/AutoCreatedBg.prefab";
            var go = new GameObject("TestGO_Nested_Bg");
            var id = go.GetEntityId();

            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{id}}}}}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Prefab should be created at nested path (background thread): {prefabPath}");
        }

        [UnityTest]
        public IEnumerator Prefab_CreateVariant_FromAssetPath_BackgroundThread()
        {
            EnsureTestFolder();

            // Step 1: create a base prefab (on main thread for setup)
            var basePath = $"{TestFolder}/Base_AssetVariant_Bg.prefab";
            var baseGo = new GameObject("TestGO_Base_AssetVariant_Bg");
            var baseId = baseGo.GetEntityId();

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{basePath}"",""gameObjectRef"":{{""instanceID"":{baseId}}},""connectGameObjectToPrefab"":false}}");

            // Step 2: create variant from background thread
            var variantPath = $"{TestFolder}/Variant_FromAsset_Bg.prefab";
            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{variantPath}"",""sourcePrefabAssetPath"":""{basePath}""}}");

            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.IsNotNull(variant, $"Variant prefab should be created at: {variantPath}");
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(variant!),
                "Created prefab from asset path should be a Prefab Variant (background thread).");
        }

        // ================================================================
        // Error cases
        // ================================================================

        static void ExpectToolErrorLogs()
        {
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));
            LogAssert.Expect(LogType.Error, new Regex("Tool execution failed"));
            LogAssert.Expect(LogType.Error, new Regex("Error Response to AI"));
        }

        [UnityTest]
        public IEnumerator Prefab_Create_EmptyPath_ReturnsError()
        {
            var go = new GameObject("TestGO_EmptyPath");
            var id = go.GetEntityId();

            ExpectToolErrorLogs();

            var jsonResult = RunToolRaw(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":"""",""gameObjectRef"":{{""instanceID"":{id}}}}}");

            StringAssert.Contains("Prefab path is empty", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Create_InvalidExtension_ReturnsError()
        {
            EnsureTestFolder();
            var go = new GameObject("TestGO_InvalidExt");
            var id = go.GetEntityId();

            ExpectToolErrorLogs();

            var jsonResult = RunToolRaw(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TestFolder}/NotAPrefab.txt"",""gameObjectRef"":{{""instanceID"":{id}}}}}");

            StringAssert.Contains("invalid", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Create_InvalidGameObjectRef_ReturnsError()
        {
            EnsureTestFolder();

            ExpectToolErrorLogs();

            var jsonResult = RunToolRaw(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TestFolder}/ShouldFail.prefab"",""gameObjectRef"":{{""instanceID"":-999999}}}}");

            StringAssert.Contains("Not found", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Create_NoGameObjectRefOrSourcePath_ReturnsError()
        {
            EnsureTestFolder();

            ExpectToolErrorLogs();

            var jsonResult = RunToolRaw(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TestFolder}/ShouldFail.prefab""}}");

            StringAssert.Contains("must be provided", jsonResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Create_InvalidSourcePrefabPath_ReturnsError()
        {
            EnsureTestFolder();

            ExpectToolErrorLogs();

            var jsonResult = RunToolRaw(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TestFolder}/ShouldFail.prefab"",""sourcePrefabAssetPath"":""{TestFolder}/NonExistent.prefab""}}");

            StringAssert.Contains("not found", jsonResult);
            yield return null;
        }
    }
}
#endif
