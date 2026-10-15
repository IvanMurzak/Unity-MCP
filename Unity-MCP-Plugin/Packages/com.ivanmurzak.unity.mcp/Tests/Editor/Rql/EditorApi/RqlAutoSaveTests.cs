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
        public void AnUntitledSceneIsReportedNotSaved()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var r = RqlUnity.Mutate("editor | call CreateGameObject(\"Loose\")");
            AssertOk(r);
            Assert.IsTrue(r.Diagnostics.Any(d => d.Code == "not-saved"), Table(r));
        }
    }
}
