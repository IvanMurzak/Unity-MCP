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
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>The <c>settings</c> source: Project Settings pages, written undoably and saved to ProjectSettings/.</summary>
    public class RqlProjectSettingsTests : AdapterTestBase
    {
        private Vector3 _gravity;
        private float _fixedDeltaTime;
        private int _vSyncCount;
        private string _productName = "";
        private EditorBuildSettingsScene[] _buildScenes = new EditorBuildSettingsScene[0];
        private const string BuildProbeFolder = "Assets/RqlBuildProbe";
        private const string LevelA = BuildProbeFolder + "/LevelA.unity";
        private const string LevelB = BuildProbeFolder + "/LevelB.unity";

        [SetUp]
        public void Remember()
        {
            _gravity = Physics.gravity;
            _fixedDeltaTime = Time.fixedDeltaTime;
            _vSyncCount = QualitySettings.vSyncCount;
            _productName = PlayerSettings.productName;
            _buildScenes = EditorBuildSettings.scenes;
        }

        [TearDown]
        public void Restore()
        {
            Physics.gravity = _gravity;
            Time.fixedDeltaTime = _fixedDeltaTime;
            QualitySettings.vSyncCount = _vSyncCount;
            PlayerSettings.productName = _productName;
            EditorBuildSettings.scenes = _buildScenes;
            AssetDatabase.DeleteAsset(BuildProbeFolder);
            // Script setters do not dirty the settings assets: dirty them, so a test that saved its value cannot leave it on disk.
            foreach (var file in new[] { "DynamicsManager", "TimeManager", "QualitySettings", "ProjectSettings", "EditorBuildSettings" })
                foreach (var asset in RqlProjectSettings.Asset(file))
                    EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void PagesAreReadable()
        {
            Physics.gravity = new Vector3(0, -9.81f, 0);
            Time.fixedDeltaTime = 0.02f;
            AssertTuple("(0, -9.81, 0)", Cell(Q("settings | select physics.gravity")));
            Assert.AreEqual(Time.fixedDeltaTime, float.Parse(Cell(Q("settings | select time.fixedDeltaTime")), System.Globalization.CultureInfo.InvariantCulture), 1e-6f);
            Assert.AreEqual(PlayerSettings.productName, Cell(Q("settings | select player.productName")));
            Assert.AreEqual(QualitySettings.names.Length.ToString(), Cell(Q("settings | select quality.levels.count()")));
        }

        [Test]
        public void GravityIsWrittenUndoablyAndSavedToProjectSettings()
        {
            Physics.gravity = new Vector3(0, -9.81f, 0);
            AssertOk(RqlUnity.Mutate("settings | set physics.gravity = \"(0, -12, 0)\""));
            Assert.AreEqual(new Vector3(0, -12, 0), Physics.gravity);
            StringAssert.Contains("m_Gravity: {x: 0, y: -12, z: 0}", File.ReadAllText("ProjectSettings/DynamicsManager.asset"));
            // A project setting is no scene edit: the scene is not marked modified.
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
            Undo.PerformUndo();
            AssertTuple("(0, -9.81, 0)", Cell(Q("settings | select physics.gravity")));
        }

        [Test]
        public void TimeQualityAndPlayerAreWrittenUndoably()
        {
            Time.fixedDeltaTime = 0.02f;
            QualitySettings.vSyncCount = 0;
            AssertOk(M("settings | set time.fixedDeltaTime = 0.01, quality.vSyncCount = 1, player.productName = \"Rql Settings Probe\""));
            Assert.AreEqual(0.01f, Time.fixedDeltaTime, 1e-6f);
            Assert.AreEqual(1, QualitySettings.vSyncCount);
            Assert.AreEqual("Rql Settings Probe", PlayerSettings.productName);
            Undo.PerformUndo();
            Assert.AreEqual(0.02f, Time.fixedDeltaTime, 1e-6f);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(_productName, PlayerSettings.productName);
        }

        [Test]
        public void AnInvalidValueChangesNothing()
        {
            Time.fixedDeltaTime = 0.02f;
            Physics.gravity = new Vector3(0, -9.81f, 0);
            var r = M("settings | set physics.gravity = \"(0, -1, 0)\", time.fixedDeltaTime = 0");
            Assert.IsFalse(r.Ok, Table(r));
            Assert.AreEqual(0.02f, Time.fixedDeltaTime, 1e-6f);
            Assert.AreEqual(new Vector3(0, -9.81f, 0), Physics.gravity);
        }

        [Test]
        public void LevelsAndColorSpaceAreReadOnly()
        {
            Assert.IsFalse(M("settings | set quality.levels = [\"A\"]").Ok);
            Assert.IsFalse(M("settings | set player.colorSpace = \"Linear\"").Ok);
        }

        [Test]
        public void SchemaListsThePages()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "settings");
            StringAssert.Contains("physics", schema);
            StringAssert.Contains("time", schema);
            StringAssert.Contains("build", schema);
        }

        // ---------------------------------------------------------------- build (scenes in the build)

        private static void SceneAssets(params string[] paths)
        {
            if (!AssetDatabase.IsValidFolder(BuildProbeFolder)) AssetDatabase.CreateFolder("Assets", "RqlBuildProbe");
            foreach (var path in paths)
            {
                // an empty scene file (the test's own scene stays untitled, so no NewScene + SaveScene here)
                File.WriteAllText(path, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
                AssetDatabase.ImportAsset(path);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), path);
            }
        }

        [Test]
        public void BuildScenesAreWrittenUndoably()
        {
            SceneAssets(LevelA, LevelB);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LevelA, true) };
            Assert.AreEqual(LevelA, Cell(Q("settings | select first=(build.scenes[0])")));

            AssertOk(M($"settings | set build.scenes = append(build.scenes, \"{LevelB}\")"));
            CollectionAssert.AreEqual(new[] { LevelA, LevelB }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
            Assert.IsTrue(EditorBuildSettings.scenes.All(s => s.enabled));
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);

            Undo.PerformUndo();
            CollectionAssert.AreEqual(new[] { LevelA }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
        }

        [Test]
        public void BuildScenesKeepDisabledEntries()
        {
            SceneAssets(LevelA, LevelB);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LevelA, true), new EditorBuildSettingsScene(LevelB, false) };
            Assert.AreEqual("1", Cell(Q("settings | select build.scenes.count()")));

            AssertOk(M($"settings | set build.scenes = [\"{LevelA}\"]"));
            Assert.AreEqual(2, EditorBuildSettings.scenes.Length);
            Assert.IsFalse(EditorBuildSettings.scenes[1].enabled);

            AssertOk(M($"settings | set build.scenes = [\"{LevelB}\", \"{LevelA}\"]"));
            CollectionAssert.AreEqual(new[] { LevelB, LevelA }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
            Assert.IsTrue(EditorBuildSettings.scenes.All(s => s.enabled));
        }

        [Test]
        public void BuildScenesRefuseMissingOrRepeatedScenes()
        {
            SceneAssets(LevelA);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LevelA, true) };
            AssertFails(M("settings | set build.scenes = [\"Assets/NoSuchScene.unity\"]"), "coercion-failed", "existing scene asset");
            AssertFails(M($"settings | set build.scenes = [\"{LevelA}\", \"{LevelA}\"]"), "coercion-failed", "each scene once");
            CollectionAssert.AreEqual(new[] { LevelA }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
        }
    }
}
