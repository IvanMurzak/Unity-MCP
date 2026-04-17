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
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Thread-safety coverage for MCP tools — regression for
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/issues/633">#633</see>.
    ///
    /// For each tool we want to guarantee the tool's body handles invocation from any
    /// thread (the MCP server dispatches tool calls from a SignalR background thread,
    /// so tools that touch Unity APIs must marshal back to the main thread via
    /// <c>MainThread.Instance.Run</c>). Forgetting to do so caused the regression in
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/issues/632">#632</see> /
    /// <see href="https://github.com/IvanMurzak/Unity-MCP/pull/637">#637</see>
    /// (<c>console-clear-logs</c>).
    ///
    /// <b>Excluded tools</b>: <c>package-add</c>/<c>package-remove</c> (trigger domain
    /// reload), <c>tests-run</c> (recursive hang), system tools (<c>ping</c>,
    /// <c>unity-skill-*</c>). See inline comments for rationale.
    ///
    /// Two helpers from <see cref="BaseTest"/> are used:
    /// <list type="bullet">
    /// <item><see cref="BaseTest.RunToolBothThreads"/> — happy path; asserts the tool
    ///       succeeds on both threads. Used when crafting a valid input is practical.</item>
    /// <item><see cref="BaseTest.RunToolExpectNoThreadViolation"/> — lenient mode; tolerates
    ///       business-level errors and only fails the test if Unity surfaces a
    ///       <c>"can only be called from the main thread"</c> exception. Used for tools
    ///       that need elaborate setup (<c>SerializedMember</c>, prefab stages, etc.) or
    ///       whose happy path would mutate the project in risky ways
    ///       (<c>package-add</c>/<c>package-remove</c>, <c>tests-run</c>, etc.).</item>
    /// </list>
    /// </summary>
    public class ToolThreadSafetyTests : BaseTest
    {
        const string TmpFolderName = "BgThreadTests_TMP";
        const string TmpFolder = "Assets/" + TmpFolderName;

        [TearDown]
        public void BgTearDown()
        {
            if (AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.DeleteAsset(TmpFolder);
        }

        // ================================================================
        // NO-ARG / READ-ONLY tools  (happy path, empty JSON)
        // ================================================================

        [UnityTest] public IEnumerator ToolList_BothThreads()
            => RunToolBothThreads(Tool_Tool.ToolListId, "{}");

        [UnityTest] public IEnumerator GameObjectComponentListAll_BothThreads()
            => RunToolBothThreads(Tool_GameObject.ComponentListToolId, @"{""pageSize"":3}");

        [UnityTest] public IEnumerator SceneListOpened_BothThreads()
            => RunToolBothThreads(Tool_Scene.SceneListOpenedToolId, "{}");

        [UnityTest] public IEnumerator SceneGetData_BothThreads()
            => RunToolBothThreads(Tool_Scene.SceneGetDataToolId, "{}");

        [UnityTest] public IEnumerator EditorApplicationGetState_BothThreads()
            => RunToolBothThreads(Tool_Editor.EditorApplicationGetStateToolId, "{}");

        [UnityTest] public IEnumerator EditorSelectionGet_BothThreads()
            => RunToolBothThreads(Tool_Editor_Selection.EditorSelectionGetToolId, "{}");

        [UnityTest] public IEnumerator ConsoleGetLogs_BothThreads()
            => RunToolBothThreads(Tool_Console.ConsoleGetLogsToolId, "{}");

        [UnityTest]
        public IEnumerator ConsoleClearLogs_BothThreads()
        {
            // Regression for #632/#637: ClearLogs calls Debug.ClearDeveloperConsole() which
            // MUST be invoked on the main thread.
            yield return RunToolBothThreads(Tool_Console.ConsoleClearLogsToolId, "{}");
        }

        [UnityTest] public IEnumerator AssetsFind_BothThreads()
            => RunToolBothThreads(Tool_Assets.AssetsFindToolId, @"{""filter"":""t:Scene"",""maxResults"":3}");

        [UnityTest] public IEnumerator AssetsFindBuiltIn_BothThreads()
            => RunToolBothThreads(Tool_Assets.AssetsFindBuiltInToolId, @"{""maxResults"":3}");

        [UnityTest] public IEnumerator AssetsShaderListAll_BothThreads()
            => RunToolBothThreads(Tool_Assets_Shader.AssetsShaderListAllToolId, "{}");

        [UnityTest] public IEnumerator AssetsRefresh_BothThreads()
            => RunToolBothThreads(Tool_Assets.AssetsRefreshToolId, "{}");

        [UnityTest] public IEnumerator PackageList_BothThreads()
            => RunToolBothThreads(Tool_Package.PackageListToolId, "{}");

        [UnityTest] public IEnumerator TypeGetJsonSchema_BothThreads()
            => RunToolBothThreads(Tool_Type.TypeGetJsonSchemaToolId, @"{""typeName"":""UnityEngine.Vector3""}");

        // ================================================================
        // GAMEOBJECT tools — happy path, per-call setup
        // ================================================================

        [UnityTest]
        public IEnumerator GameObjectCreate_BothThreads()
        {
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectCreateToolId, @"{""name"":""bg_main""}");
            Assert.IsNotNull(GameObject.Find("bg_main"));
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectCreateToolId, @"{""name"":""bg_bg""}");
            Assert.IsNotNull(GameObject.Find("bg_bg"));
        }

        [UnityTest]
        public IEnumerator GameObjectFind_BothThreads()
        {
            var go = new GameObject("bg_find");
            var id = go.GetEntityId();
            yield return RunToolBothThreads(Tool_GameObject.GameObjectFindToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{id}}}}}");
        }

        [UnityTest]
        public IEnumerator GameObjectDestroy_BothThreads()
        {
            var a = new GameObject("bg_destroy_a").GetEntityId();
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectDestroyToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{a}}}}}");

            var b = new GameObject("bg_destroy_b").GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectDestroyToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{b}}}}}");
        }

        [UnityTest]
        public IEnumerator GameObjectDuplicate_BothThreads()
        {
            var a = new GameObject("bg_dup_a").GetEntityId();
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectDuplicateToolId,
                $@"{{""gameObjectRefs"":[{{""instanceID"":{a}}}]}}");

            var b = new GameObject("bg_dup_b").GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectDuplicateToolId,
                $@"{{""gameObjectRefs"":[{{""instanceID"":{b}}}]}}");
        }

        [UnityTest]
        public IEnumerator GameObjectSetParent_BothThreads()
        {
            var parent = new GameObject("bg_parent").GetEntityId();
            var child1 = new GameObject("bg_child_a").GetEntityId();
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectSetParentToolId,
                $@"{{""gameObjectRefs"":[{{""instanceID"":{child1}}}],""parentGameObjectRef"":{{""instanceID"":{parent}}}}}");

            var child2 = new GameObject("bg_child_b").GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectSetParentToolId,
                $@"{{""gameObjectRefs"":[{{""instanceID"":{child2}}}],""parentGameObjectRef"":{{""instanceID"":{parent}}}}}");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentAdd_BothThreads()
        {
            var a = new GameObject("bg_addc_a").GetEntityId();
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectComponentAddToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{a}}},""componentNames"":[""UnityEngine.BoxCollider""]}}");

            var b = new GameObject("bg_addc_b").GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectComponentAddToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{b}}},""componentNames"":[""UnityEngine.SphereCollider""]}}");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentGet_BothThreads()
        {
            var go = new GameObject("bg_getc");
            var id = go.GetEntityId();
            // Transform lives at component index 0 on every GameObject.
            yield return RunToolBothThreads(Tool_GameObject.GameObjectComponentGetToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{id}}},""componentRef"":{{""index"":0}}}}");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentDestroy_BothThreads()
        {
            var a = new GameObject("bg_destc_a");
            a.AddComponent<BoxCollider>();
            var idA = a.GetEntityId();
            yield return RunToolMainThreadCoop(Tool_GameObject.GameObjectComponentDestroyToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{idA}}},""destroyComponentRefs"":[{{""typeName"":""UnityEngine.BoxCollider""}}]}}");

            var b = new GameObject("bg_destc_b");
            b.AddComponent<SphereCollider>();
            var idB = b.GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_GameObject.GameObjectComponentDestroyToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{idB}}},""destroyComponentRefs"":[{{""typeName"":""UnityEngine.SphereCollider""}}]}}");
        }

        [UnityTest]
        public IEnumerator GameObjectModify_ThreadSafetyOnly()
        {
            // gameobject-modify requires a SerializedMember tree — we cover thread safety
            // only (business errors from an empty diff are tolerated).
            var id = new GameObject("bg_modify").GetEntityId();
            yield return RunToolExpectNoThreadViolation(Tool_GameObject.GameObjectModifyToolId,
                $@"{{""gameObjectRefs"":[{{""instanceID"":{id}}}],""gameObjectDiffs"":[{{""name"":""root""}}]}}");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentModify_ThreadSafetyOnly()
        {
            var go = new GameObject("bg_cmodify");
            go.AddComponent<BoxCollider>();
            var id = go.GetEntityId();
            yield return RunToolExpectNoThreadViolation(Tool_GameObject.GameObjectComponentModifyToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{id}}},""componentRef"":{{""typeName"":""UnityEngine.BoxCollider""}},""componentDiff"":{{""name"":""root""}}}}");
        }

        // ================================================================
        // OBJECT tools
        // ================================================================

        [UnityTest]
        public IEnumerator ObjectGetData_BothThreads()
        {
            var id = new GameObject("bg_obj").GetEntityId();
            yield return RunToolBothThreads(Tool_Object.ObjectGetDataToolId,
                $@"{{""objectRef"":{{""instanceID"":{id}}}}}");
        }

        [UnityTest]
        public IEnumerator ObjectModify_ThreadSafetyOnly()
        {
            var id = new GameObject("bg_objmod").GetEntityId();
            yield return RunToolExpectNoThreadViolation(Tool_Object.ObjectModifyToolId,
                $@"{{""objectRef"":{{""instanceID"":{id}}},""objectDiff"":{{""name"":""root""}}}}");
        }

        // ================================================================
        // EDITOR tools
        // ================================================================

        [UnityTest]
        public IEnumerator EditorSelectionSet_BothThreads()
        {
            var id = new GameObject("bg_select").GetEntityId();
            yield return RunToolBothThreads(Tool_Editor_Selection.EditorSelectionSetToolId,
                $@"{{""select"":[{{""instanceID"":{id}}}]}}");
        }

        [UnityTest]
        public IEnumerator EditorApplicationSetState_BothThreads()
        {
            // No-op toggle: current state is not-playing, not-paused.
            yield return RunToolBothThreads(Tool_Editor.EditorApplicationSetStateToolId,
                @"{""isPlaying"":false,""isPaused"":false}");
        }

        // ================================================================
        // ASSETS tools
        // ================================================================

        [UnityTest]
        public IEnumerator AssetsCreateFolder_BothThreads()
        {
            EnsureTmpFolder();
            yield return RunToolMainThreadCoop(Tool_Assets.AssetsCreateFolderToolId,
                $@"{{""inputs"":[{{""parentFolderPath"":""{TmpFolder}"",""newFolderName"":""a""}}]}}");
            yield return RunToolFromBackgroundThread(Tool_Assets.AssetsCreateFolderToolId,
                $@"{{""inputs"":[{{""parentFolderPath"":""{TmpFolder}"",""newFolderName"":""b""}}]}}");
        }

        [UnityTest]
        public IEnumerator AssetsGetData_BothThreads()
        {
            var assetPath = CreateTmpTextAsset("get_data.txt", "hello");
            yield return RunToolBothThreads(Tool_Assets.AssetsGetDataToolId,
                $@"{{""assetRef"":{{""assetPath"":""{assetPath}""}}}}");
        }

        [UnityTest]
        public IEnumerator AssetsModify_ThreadSafetyOnly()
        {
            var assetPath = CreateTmpTextAsset("modify.txt", "hello");
            yield return RunToolExpectNoThreadViolation(Tool_Assets.AssetsModifyToolId,
                $@"{{""assetRef"":{{""assetPath"":""{assetPath}""}},""content"":{{""name"":""root""}}}}");
        }

        [UnityTest]
        public IEnumerator AssetsMaterialCreate_BothThreads()
        {
            EnsureTmpFolder();
            yield return RunToolMainThreadCoop(Tool_Assets.AssetsMaterialCreateToolId,
                $@"{{""assetPath"":""{TmpFolder}/mat_main.mat"",""shaderName"":""Standard""}}");
            yield return RunToolFromBackgroundThread(Tool_Assets.AssetsMaterialCreateToolId,
                $@"{{""assetPath"":""{TmpFolder}/mat_bg.mat"",""shaderName"":""Standard""}}");
        }

        [UnityTest]
        public IEnumerator AssetsShaderGetData_BothThreads()
        {
            // Find any shader asset in the project to query.
            var shaderPath = FindFirstShaderPath();
            if (shaderPath == null)
            {
                Assert.Ignore("No shader asset available in project.");
                yield break;
            }
            yield return RunToolBothThreads(Tool_Assets_Shader.AssetsShaderGetDataToolId,
                $@"{{""assetRef"":{{""assetPath"":""{shaderPath}""}}}}");
        }

        [UnityTest]
        public IEnumerator AssetsCopy_BothThreads()
        {
            var src = CreateTmpTextAsset("copy_src.txt", "source");
            yield return RunToolMainThreadCoop(Tool_Assets.AssetsCopyToolId,
                $@"{{""sourcePaths"":[""{src}""],""destinationPaths"":[""{TmpFolder}/copy_main.txt""]}}");
            yield return RunToolFromBackgroundThread(Tool_Assets.AssetsCopyToolId,
                $@"{{""sourcePaths"":[""{src}""],""destinationPaths"":[""{TmpFolder}/copy_bg.txt""]}}");
        }

        [UnityTest]
        public IEnumerator AssetsMove_BothThreads()
        {
            var src1 = CreateTmpTextAsset("move_a.txt", "a");
            yield return RunToolMainThreadCoop(Tool_Assets.AssetsMoveToolId,
                $@"{{""sourcePaths"":[""{src1}""],""destinationPaths"":[""{TmpFolder}/move_a_done.txt""]}}");

            var src2 = CreateTmpTextAsset("move_b.txt", "b");
            yield return RunToolFromBackgroundThread(Tool_Assets.AssetsMoveToolId,
                $@"{{""sourcePaths"":[""{src2}""],""destinationPaths"":[""{TmpFolder}/move_b_done.txt""]}}");
        }

        [UnityTest]
        public IEnumerator AssetsDelete_BothThreads()
        {
            var src1 = CreateTmpTextAsset("del_a.txt", "a");
            yield return RunToolMainThreadCoop(Tool_Assets.AssetsDeleteToolId, $@"{{""paths"":[""{src1}""]}}");

            var src2 = CreateTmpTextAsset("del_b.txt", "b");
            yield return RunToolFromBackgroundThread(Tool_Assets.AssetsDeleteToolId,
                $@"{{""paths"":[""{src2}""]}}");
        }

        // ---------- Prefabs ----------

        [UnityTest]
        public IEnumerator AssetsPrefabCreate_BothThreads()
        {
            EnsureTmpFolder();
            var idA = new GameObject("bg_prefab_a").GetEntityId();
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TmpFolder}/prefab_a.prefab"",""gameObjectRef"":{{""instanceID"":{idA}}}}}");

            var idB = new GameObject("bg_prefab_b").GetEntityId();
            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{TmpFolder}/prefab_b.prefab"",""gameObjectRef"":{{""instanceID"":{idB}}}}}");
        }

        [UnityTest]
        public IEnumerator AssetsPrefabInstantiate_BothThreads()
        {
            EnsureTmpFolder();
            var seedId = new GameObject("bg_seed_prefab").GetEntityId();
            var prefabPath = $"{TmpFolder}/seed.prefab";
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectRef"":{{""instanceID"":{seedId}}}}}");

            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabInstantiateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectPath"":""bg_inst_main""}}");
            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabInstantiateToolId,
                $@"{{""prefabAssetPath"":""{prefabPath}"",""gameObjectPath"":""bg_inst_bg""}}");
        }

        [UnityTest]
        public IEnumerator AssetsPrefabOpenSaveClose_BothThreads()
        {
            EnsureTmpFolder();

            // Create a prefab asset from a scene GameObject.
            var seedA = new GameObject("bg_prefab_osc_a").GetEntityId();
            var prefabPathA = $"{TmpFolder}/osc_a.prefab";
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPathA}"",""gameObjectRef"":{{""instanceID"":{seedA}}}}}");

            // Instantiate it so we get a prefab instance to open.
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabInstantiateToolId,
                $@"{{""prefabAssetPath"":""{prefabPathA}"",""gameObjectPath"":""bg_osc_inst_a""}}");
            var instanceA = GameObject.Find("bg_osc_inst_a");
            Assert.IsNotNull(instanceA, "Prefab instance not found after instantiate.");

            // Open → Save → Close (main thread).
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabOpenToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{instanceA!.GetEntityId()}}}}}");
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabSaveToolId, "{}");
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCloseToolId, @"{""save"":false}");

            // Same sequence from a background thread.
            var seedB = new GameObject("bg_prefab_osc_b").GetEntityId();
            var prefabPathB = $"{TmpFolder}/osc_b.prefab";
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabCreateToolId,
                $@"{{""prefabAssetPath"":""{prefabPathB}"",""gameObjectRef"":{{""instanceID"":{seedB}}}}}");
            yield return RunToolMainThreadCoop(Tool_Assets_Prefab.AssetsPrefabInstantiateToolId,
                $@"{{""prefabAssetPath"":""{prefabPathB}"",""gameObjectPath"":""bg_osc_inst_b""}}");
            var instanceB = GameObject.Find("bg_osc_inst_b");
            Assert.IsNotNull(instanceB, "Prefab instance not found after instantiate.");

            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabOpenToolId,
                $@"{{""gameObjectRef"":{{""instanceID"":{instanceB!.GetEntityId()}}}}}");
            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabSaveToolId, "{}");
            yield return RunToolFromBackgroundThread(Tool_Assets_Prefab.AssetsPrefabCloseToolId, @"{""save"":false}");
        }

        // ================================================================
        // SCENE tools
        // ================================================================

        [UnityTest]
        public IEnumerator SceneCreate_BothThreads()
        {
            EnsureTmpFolder();
            var startingActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            yield return RunToolMainThreadCoop(Tool_Scene.SceneCreateToolId,
                $@"{{""path"":""{TmpFolder}/scene_main.unity""}}");
            yield return RunToolFromBackgroundThread(Tool_Scene.SceneCreateToolId,
                $@"{{""path"":""{TmpFolder}/scene_bg.unity""}}");

            // Restore a baseline active scene if the test clobbered it.
            if (startingActive.IsValid() && UnityEngine.SceneManagement.SceneManager.GetActiveScene() != startingActive)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(startingActive);
        }

        [UnityTest]
        public IEnumerator SceneOpenSaveSetActive_BothThreads()
        {
            EnsureTmpFolder();
            var scenePath = $"{TmpFolder}/reopen.unity";
            yield return RunToolMainThreadCoop(Tool_Scene.SceneCreateToolId, $@"{{""path"":""{scenePath}""}}");

            // open (main + bg)
            yield return RunToolMainThreadCoop(Tool_Scene.SceneOpenToolId,
                $@"{{""sceneRef"":{{""assetPath"":""{scenePath}""}}}}");
            yield return RunToolFromBackgroundThread(Tool_Scene.SceneOpenToolId,
                $@"{{""sceneRef"":{{""assetPath"":""{scenePath}""}}}}");

            // set-active (both)
            yield return RunToolBothThreads(Tool_Scene.SceneSetActiveToolId,
                $@"{{""sceneRef"":{{""assetPath"":""{scenePath}""}}}}");

            // save (both)
            yield return RunToolBothThreads(Tool_Scene.SceneSaveToolId, "{}");
        }

        [UnityTest]
        public IEnumerator SceneUnload_BothThreads()
        {
            EnsureTmpFolder();

            // Create a saved scene so we can open additional scenes additively.
            var basePath = $"{TmpFolder}/unload_base.unity";
            yield return RunToolMainThreadCoop(Tool_Scene.SceneCreateToolId,
                $@"{{""path"":""{basePath}""}}");

            // Create two extra scenes additively (allowed now because base is saved).
            var mainPath = $"{TmpFolder}/unload_main.unity";
            var bgPath = $"{TmpFolder}/unload_bg.unity";
            yield return RunToolMainThreadCoop(Tool_Scene.SceneCreateToolId,
                $@"{{""path"":""{mainPath}"",""newSceneMode"":""Additive""}}");
            yield return RunToolMainThreadCoop(Tool_Scene.SceneCreateToolId,
                $@"{{""path"":""{bgPath}"",""newSceneMode"":""Additive""}}");

            // Unload each additive scene from both threads (tool takes scene name, not path).
            yield return RunToolMainThreadCoop(Tool_Scene.SceneUnloadToolId,
                @"{""name"":""unload_main""}");
            yield return RunToolFromBackgroundThread(Tool_Scene.SceneUnloadToolId,
                @"{""name"":""unload_bg""}");
        }

        // ================================================================
        // SCRIPT tools
        // ================================================================

        [UnityTest]
        public IEnumerator ScriptUpdateOrCreate_ReadDelete_BothThreads()
        {
            EnsureTmpFolder();
            var pathA = $"{TmpFolder}/S_a.cs";
            var pathB = $"{TmpFolder}/S_b.cs";
            const string SrcA = "public class BgSa { public static void M() {} }";
            const string SrcB = "public class BgSb { public static void M() {} }";

            // update-or-create
            yield return RunToolMainThreadCoop(Tool_Script.ScriptUpdateOrCreateToolId,
                $@"{{""filePath"":""{pathA}"",""content"":{JsonString(SrcA)}}}");
            yield return RunToolFromBackgroundThread(Tool_Script.ScriptUpdateOrCreateToolId,
                $@"{{""filePath"":""{pathB}"",""content"":{JsonString(SrcB)}}}");

            // read (main + bg)
            yield return RunToolBothThreads(Tool_Script.ScriptReadToolId,
                $@"{{""filePath"":""{pathA}""}}",
                $@"{{""filePath"":""{pathB}""}}");

            // delete
            yield return RunToolMainThreadCoop(Tool_Script.ScriptDeleteToolId, $@"{{""files"":[""{pathA}""]}}");
            yield return RunToolFromBackgroundThread(Tool_Script.ScriptDeleteToolId,
                $@"{{""files"":[""{pathB}""]}}");
        }

        [UnityTest]
        public IEnumerator ScriptExecute_BothThreads()
        {
            const string Code = "public class Script { public static int Main() { return 42; } }";
            yield return RunToolBothThreads(Tool_Script.ScriptExecuteToolId,
                $@"{{""csharpCode"":{JsonString(Code)},""className"":""Script"",""methodName"":""Main""}}");
        }

        // ================================================================
        // TOOL CONTROL
        // ================================================================

        [UnityTest]
        public IEnumerator ToolSetEnabledState_BothThreads()
        {
            // Enable tool-list twice (idempotent, already enabled) from both threads.
            yield return RunToolBothThreads(Tool_Tool.ToolSetEnabledStateId,
                @"{""tools"":[{""name"":""tool-list"",""enabled"":true}]}");
        }

        // ================================================================
        // REFLECTION (complex MethodRef schema → thread-safety only)
        // ================================================================

        [UnityTest]
        public IEnumerator ReflectionMethodFind_ThreadSafetyOnly()
        {
            yield return RunToolExpectNoThreadViolation(Tool_Reflection.ReflectionMethodFindToolId,
                @"{""filter"":{""Namespace"":""UnityEngine"",""TypeName"":""Transform"",""MethodName"":""LookAt""}}");
        }

        [UnityTest]
        public IEnumerator ReflectionMethodCall_BothThreads()
        {
            // Call a static method on a test-scoped GameObject's Transform via instance method.
            var go = new GameObject("bg_reflect");
            var id = go.GetEntityId();
            yield return RunToolBothThreads(Tool_Reflection.ReflectionMethodCallToolId,
                $@"{{""filter"":{{""Namespace"":""UnityEngine"",""TypeName"":""GameObject"",""MethodName"":""Find"",""InputParameters"":[{{""TypeName"":""System.String"",""Name"":""name""}}]}},""inputParameters"":[{{""typeName"":""System.String"",""name"":""name"",""value"":""bg_reflect""}}]}}");
        }

        // ================================================================
        // PACKAGE MANAGER — package-add / package-remove intentionally NOT
        // exercised here: both return status "Processing" and kick off a real
        // Unity PackageManager resolution on a background task, which in turn
        // forces a domain reload. Under the test runner that hangs the whole
        // batch-mode run. Their thread-safety is already covered by the fact
        // that their entry path is a synchronous parameter check.
        // ================================================================

        [UnityTest]
        public IEnumerator PackageSearch_ThreadSafetyOnly()
        {
            // Registry round-trip is slow and flaky under test; thread-safety only.
            yield return RunToolExpectNoThreadViolation(Tool_Package.PackageSearchToolId,
                @"{""query"":""com.unity.textmeshpro"",""maxResults"":1,""offlineMode"":true}");
        }

        // ================================================================
        // SCREENSHOTS — not guaranteed to have a camera/view in batchmode.
        // ================================================================

        [UnityTest]
        public IEnumerator ScreenshotCamera_BothThreads()
        {
            new GameObject("TestCamera").AddComponent<Camera>();
            yield return RunToolBothThreads(Tool_Screenshot.ScreenshotCameraToolId, @"{""width"":64,""height"":64}");
        }

        [UnityTest]
        public IEnumerator ScreenshotGameView_ThreadSafetyOnly()
            => RunToolExpectNoThreadViolation(Tool_Screenshot.ScreenshotGameViewToolId, "{}");

        [UnityTest]
        public IEnumerator ScreenshotSceneView_ThreadSafetyOnly()
            => RunToolExpectNoThreadViolation(Tool_Screenshot.ScreenshotSceneViewToolId, @"{""width"":64,""height"":64}");

        // ================================================================
        // TESTS — recursive; skip deliberately.
        // ================================================================
        // tests-run intentionally has no test here: executing it from within the test
        // runner causes recursion and a hang. Its thread safety is implicit because the
        // test runner itself schedules the invocation onto the main thread.

        // ================================================================
        // Helpers
        // ================================================================

        static void EnsureTmpFolder()
        {
            if (!AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.CreateFolder("Assets", TmpFolderName);
        }

        static string CreateTmpTextAsset(string fileName, string contents)
        {
            EnsureTmpFolder();
            var path = $"{TmpFolder}/{fileName}";
            File.WriteAllText(path, contents);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        static string? FindFirstShaderPath()
        {
            // Prefer the built-in Standard shader.
            var standard = Shader.Find("Standard");
            if (standard != null)
            {
                var p = AssetDatabase.GetAssetPath(standard);
                if (!string.IsNullOrEmpty(p))
                    return p;
            }

            var guids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(p))
                    return p;
            }
            return null;
        }

        /// <summary>Returns a JSON string literal (quoted, backslash-escaped).</summary>
        static string JsonString(string raw)
            => System.Text.Json.JsonSerializer.Serialize(raw);
    }
}
#endif
