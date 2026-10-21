/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    using CollectObjects = global::Unity.AI.Navigation.CollectObjects;
    using NavMeshSurface = global::Unity.AI.Navigation.NavMeshSurface;

    public class Unity_AI_Navigation_NavMeshSurface_AdapterTests : NavigationTestBase
    {
        [Test]
        public void AddComponentThenConfigure()
        {
            new GameObject("Level");
            Ok(M("objects | where name == \"Level\" | call AddComponent(\"NavMeshSurface\")"));
            var r = M(Surfaces + " | set collectObjects = \"Volume\", size = \"(20, 5, 20)\", center = \"(0, 1, 0)\", layerMask = 1, defaultAreaName = \"Not Walkable\"");
            Ok(r);
            var s = Object.FindAnyObjectByType<NavMeshSurface>();
            Assert.AreEqual(CollectObjects.Volume, s.collectObjects);
            Assert.AreEqual(new Vector3(20, 5, 20), s.size);
            Assert.AreEqual(new Vector3(0, 1, 0), s.center);
            Assert.AreEqual(1, s.layerMask.value);
            Assert.AreEqual(1, s.defaultArea);
            Assert.AreEqual("Not Walkable", Cell(Q(Surfaces + " | select defaultAreaName")));
        }

        [Test]
        public void AgentTypeIsAName()
        {
            var s = CreateGround();
            Assert.AreEqual("Humanoid", Cell(Q(Surfaces + " | select agentType")));
            s.agentTypeID = 12345;
            Assert.AreEqual("#12345", Cell(Q(Surfaces + " | select agentType")));
            Ok(M(Surfaces + " | set agentType = \"humanoid\""));
            Assert.AreEqual(0, s.agentTypeID);
        }

        [Test]
        public void UnknownAgentTypeChangesNothing()
        {
            var s = CreateGround();
            s.defaultArea = 2;
            var r = M(Surfaces + " | set defaultArea = 0, agentType = \"Giant\"");
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("Giant", Table(r));
            StringAssert.Contains("Humanoid", Table(r));
            Assert.AreEqual(2, s.defaultArea, "all-or-nothing");
        }

        [Test]
        public void VoxelSizeOverrideAndEffectiveBuildSettings()
        {
            CreateGround();
            Ok(M(Surfaces + " | set overrideVoxelSize = true, voxelSize = 0.1, overrideTileSize = true, tileSize = 128"));
            var r = Q(Surfaces + " | select buildSettings.voxelSize buildSettings.tileSize buildSettings.agentRadius");
            Ok(r);
            Assert.AreEqual(0.1, r.Rows![0][0]!.GetValue<double>(), 1e-6);
            Assert.AreEqual(128, r.Rows![0][1]!.GetValue<long>());
            Assert.AreEqual(NavMesh.GetSettingsByID(0).agentRadius, r.Rows![0][2]!.GetValue<double>(), 1e-6);
        }

        [Test]
        public void BakeAndClear()
        {
            var s = CreateGround();
            Ok(M(Surfaces + " | call Bake()"));
            Assert.IsNotNull(s.navMeshData);
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(1, 0, 1), out _, 0.5f, NavMesh.AllAreas), "the plane is walkable after Bake()");
            Assert.AreNotEqual("null", Cell(Q(Surfaces + " | select navMeshData")));

            Ok(M(Surfaces + " | call Clear()"));
            Assert.IsNull(s.navMeshData);
            Assert.IsFalse(NavMesh.SamplePosition(new Vector3(1, 0, 1), out _, 0.5f, NavMesh.AllAreas));
        }

        [Test]
        public void BakeInASavedSceneStoresTheNavMeshAsset()
        {
            const string folder = "Assets/RqlNavigationTests";
            AssetDatabase.DeleteAsset(folder);
            AssetDatabase.CreateFolder("Assets", "RqlNavigationTests");
            try
            {
                var s = CreateGround();
                Assert.IsTrue(EditorSceneManager.SaveScene(s.gameObject.scene, folder + "/Level.unity"));
                Ok(M(Surfaces + " | call Bake()"));
                var path = AssetDatabase.GetAssetPath(s.navMeshData);
                Assert.AreEqual(folder + "/Level/NavMesh-Ground.asset", path);

                Ok(M(Surfaces + " | call Bake()"));
                Assert.AreEqual(path, AssetDatabase.GetAssetPath(s.navMeshData), "a rebake replaces its own asset");

                Ok(M(Surfaces + " | call Clear()"));
                Assert.IsNull(s.navMeshData);
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<NavMeshData>(path), "Clear() deletes the asset Bake() created");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void WritesAreUndoable()
        {
            var s = CreateGround();
            Ok(M(Surfaces + " | set collectObjects = \"Children\", defaultArea = 2"));
            Undo.PerformUndo();
            Assert.AreEqual(CollectObjects.All, s.collectObjects);
            Assert.AreEqual(0, s.defaultArea);
        }

        [Test]
        public void BackingFieldsAreHidden()
        {
            CreateGround();
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "NavMeshSurface");
            StringAssert.DoesNotContain("m_AgentTypeID", schema);
            StringAssert.DoesNotContain("m_SerializedVersion", schema);
            StringAssert.Contains("agentType", schema);
            StringAssert.Contains("Bake()", schema);
            StringAssert.Contains("Clear()", schema);
            var row = Q(Surfaces);
            Ok(row);
            StringAssert.DoesNotContain("m_", Table(row));
        }
    }
}
