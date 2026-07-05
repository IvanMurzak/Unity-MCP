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
using System.IO;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestGameObjectUtils
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[{nameof(TestGameObjectUtils)}] SetUp");
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[{nameof(TestGameObjectUtils)}] TearDown");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FindByPath()
        {
            var parentName = "root";
            var childName = "nestedGo";
            new GameObject(parentName).AddChild(childName);

            var prefixes = new[] { "", "/" };
            foreach (var prefix in prefixes)
            {
                Assert.IsNotNull(GameObjectUtils.FindByPath($"{prefix}{parentName}"), $"{prefix}{parentName} should not be null");
                Assert.IsNotNull(GameObjectUtils.FindByPath($"{prefix}{parentName}/{childName}"), $"{prefix}{parentName}/{childName} should not be null");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetPath()
        {
            var parentName = "root";
            var childName = "nestedGo";
            var child = new GameObject(parentName).AddChild(childName);

            Assert.AreEqual(child.GetPath(), $"{parentName}/{childName}",
                $"GameObject '{childName}' should have path '{parentName}/{childName}'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator FindByName_AdditiveScene_ObjectIsFound()
        {
            var objectName = "additiveSceneObject_" + System.Guid.NewGuid().ToString("N");

            // EditorSceneManager refuses to add a scene additively while the active
            // scene is untitled/unsaved, which is how the Test Runner starts each
            // EditMode test. Give the active scene a temp saved path first.
            var activeScene = EditorSceneManager.GetActiveScene();
            var tempScenePath = string.IsNullOrEmpty(activeScene.path)
                ? "Assets/__TempTestScene_" + System.Guid.NewGuid().ToString("N") + ".unity"
                : null;
            if (tempScenePath != null)
                EditorSceneManager.SaveScene(activeScene, tempScenePath);

            // SceneManager.CreateScene is Play-Mode-only; in Edit Mode, additive
            // scenes are created via EditorSceneManager instead.
            var additiveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var go = new GameObject(objectName);
                SceneManager.MoveGameObjectToScene(go, additiveScene);

                // NewScene(..., Additive) makes the new scene active by default, which
                // would defeat this test (the object would end up in the active scene
                // after all). Restore the original scene as active so additiveScene is
                // genuinely loaded-but-not-active, reproducing the bug where
                // FindRootGameObjects() only looked at GetActiveScene().
                SceneManager.SetActiveScene(activeScene);
                Assert.IsNotNull(GameObjectUtils.FindByName(objectName),
                    $"'{objectName}' lives in a loaded-but-not-active scene and should still be found");
            }
            finally
            {
                // EditorSceneManager.CloseScene is synchronous (unlike Play-Mode's
                // UnloadSceneAsync), so it's safe inside a finally block and runs
                // even when the assert above fails, so no temp scene asset leaks.
                EditorSceneManager.CloseScene(additiveScene, removeScene: true);
                if (tempScenePath != null && File.Exists(tempScenePath))
                    AssetDatabase.DeleteAsset(tempScenePath);
            }
            yield return null;
        }
    }
}
