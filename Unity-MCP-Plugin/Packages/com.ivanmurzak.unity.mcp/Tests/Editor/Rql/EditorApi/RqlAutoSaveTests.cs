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
using System.IO;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary><see cref="RqlUnity.Mutate"/> (the rql-mutate tool) writes its changes to disk without a separate save call.</summary>
    public class RqlAutoSaveTests : EditorApiTestBase
    {
        [Test]
        public void SceneChangesAreSaved()
        {
            var path = $"{Folder}/Saved.unity";
            AssertOk(M($"editor | call NewScene(\"{path}\", true)"));
            AssertOk(RqlUnity.Mutate("editor | call CreateGameObject(\"Persisted\")"));
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
            StringAssert.Contains("m_Name: Persisted", File.ReadAllText(path));
        }

        [Test]
        public void AssetChangesAreSaved()
        {
            var path = $"{Folder}/Saved.mat";
            AssertOk(M($"editor | call CreateMaterial(\"{path}\")"));
            AssertOk(RqlUnity.Mutate($"unity.asset(\"{path}\") | set renderQueue = 2100"));
            StringAssert.Contains("m_CustomRenderQueue: 2100", File.ReadAllText(path));
        }

        [Test]
        public void ASelectionChangeDoesNotSaveUnrelatedSceneEdits()
        {
            var path = $"{Folder}/Untouched.unity";
            AssertOk(M($"editor | call NewScene(\"{path}\", true)"));
            var scene = SceneManager.GetActiveScene();
            new UnityEngine.GameObject("UnsavedEdit");
            EditorSceneManager.MarkSceneDirty(scene);
            AssertOk(RqlUnity.Mutate("editor | set selection = []"));
            Assert.IsTrue(scene.isDirty);
            StringAssert.DoesNotContain("UnsavedEdit", File.ReadAllText(path));
        }

        [Test]
        public void ModulesHearWhetherAMutationSavedOrDiscarded()
        {
            int saving = 0, discarded = 0;
            void OnSaving() => saving++;
            void OnDiscarded() => discarded++;
            RqlUnity.Saving += OnSaving;
            RqlUnity.Discarded += OnDiscarded;
            try
            {
                new UnityEngine.GameObject("Target");
                RqlUnity.Mutate("objects | where name == \"Nobody\" | set name = \"X\"");   // nothing applied
                Assert.AreEqual((0, 1), (saving, discarded));
                RqlUnity.Mutate("objects | where name == \"Target\" | set name = \"X\"", dryRun: true);
                Assert.AreEqual((0, 2), (saving, discarded));
                RqlUnity.Mutate("objects | where");                                              // parse error
                Assert.AreEqual((0, 3), (saving, discarded));
                RqlUnity.Mutate("objects | where name == \"Target\" | set name = \"Renamed\"");   // applied
                Assert.AreEqual((1, 3), (saving, discarded));
            }
            finally
            {
                RqlUnity.Saving -= OnSaving;
                RqlUnity.Discarded -= OnDiscarded;
            }
        }

        [Test]
        public void AnUntitledSceneIsReportedNotSaved()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var r = RqlUnity.Mutate("editor | call CreateGameObject(\"Loose\")");
            AssertOk(r);
            Assert.IsTrue(r.Diagnostics.Any(d => d.Code == "not-saved"), Table(r));
        }
    }
}
