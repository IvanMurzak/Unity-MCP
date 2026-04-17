/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests for the precondition check in <c>tests-run</c>: the tool must throw
    /// <see cref="InvalidOperationException"/> if any currently open scene has
    /// unsaved changes, so the caller can save them before retrying.
    /// </summary>
    public class TestsRunDirtySceneTests
    {
        // The active scene is reused across tests in the Editor test runner, so we
        // must always leave it non-dirty on teardown. We achieve that by creating a
        // fresh empty untitled scene both before and after each test, which
        // guarantees no accidental cross-test pollution.

        [SetUp]
        public void EnsureCleanScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void RestoreCleanScene()
        {
            // Reset to a fresh empty scene so the next test (or the next session)
            // never sees the dirty marker we may have set.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void GetDirtyOpenScenes_WhenNoSceneIsDirty_ReturnsEmptyList()
        {
            // Fresh empty scene from SetUp — nothing dirty.
            var dirty = Tool_Tests.GetDirtyOpenScenes();

            Assert.IsNotNull(dirty);
            Assert.AreEqual(0, dirty.Count,
                "Expected no dirty scenes immediately after creating a fresh empty scene.");
        }

        [Test]
        public void ThrowIfAnyOpenSceneIsDirty_WhenNoSceneIsDirty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Tool_Tests.ThrowIfAnyOpenSceneIsDirty());
        }

        [Test]
        public void ThrowIfAnyOpenSceneIsDirty_WhenActiveSceneIsDirty_Throws()
        {
            MarkActiveSceneDirty();

            var ex = Assert.Throws<InvalidOperationException>(
                () => Tool_Tests.ThrowIfAnyOpenSceneIsDirty());

            Assert.IsNotNull(ex);
            StringAssert.Contains("Cannot run tests", ex!.Message);
            StringAssert.Contains("unsaved changes", ex.Message);
            StringAssert.Contains("Save the scene", ex.Message);
        }

        [Test]
        public void ThrowIfAnyOpenSceneIsDirty_Message_ContainsDirtySceneCount()
        {
            MarkActiveSceneDirty();

            var ex = Assert.Throws<InvalidOperationException>(
                () => Tool_Tests.ThrowIfAnyOpenSceneIsDirty());

            Assert.IsNotNull(ex);
            // The untitled fresh scene counts as 1 dirty scene; the message must
            // surface that count so the caller can see how many scenes to save.
            StringAssert.Contains("1 open scene(s)", ex!.Message);
        }

        [Test]
        public void GetDirtyOpenScenes_WhenActiveSceneIsDirty_ReturnsThatScene()
        {
            MarkActiveSceneDirty();

            var dirty = Tool_Tests.GetDirtyOpenScenes();

            Assert.AreEqual(1, dirty.Count,
                "Exactly one open scene should be reported as dirty.");
            Assert.IsTrue(dirty[0].isDirty,
                "The returned scene must actually be dirty.");
        }

        // --- helpers ---------------------------------------------------------

        /// <summary>
        /// Marks the active scene as dirty by creating a GameObject inside it.
        /// Any change to scene content flips the <c>isDirty</c> flag.
        /// </summary>
        static void MarkActiveSceneDirty()
        {
            // Spawn a GameObject — this is the simplest reliable way to make a
            // scene dirty without depending on EditorSceneManager.MarkSceneDirty
            // being available in every test configuration.
            var go = new GameObject("TestsRunDirtyScene_Marker");
            // Ensure it's parented in the active scene (default behavior).
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, activeScene);
            // Belt-and-braces: also explicitly mark the scene dirty.
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
}
