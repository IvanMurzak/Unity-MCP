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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Physics3D
{
    public class UnityEngine_Collider_AdapterTests
    {
        private BoxCollider _box = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            _box = new GameObject("Crate").AddComponent<BoxCollider>();
        }

        private static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        [Test]
        public void ThePhysicsModuleIsDiscovered()
        {
            CollectionAssert.Contains(RqlUnity.Engine.Modules.Select(m => m.Module).ToList(), typeof(PhysicsModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void ReadingCollidersNeverInstantiatesPhysicsMaterials()
        {
            var r = RqlUnity.Engine.Execute("objects | get components | where $ is BoxCollider");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.IsTrue(RqlUnity.Engine.Execute("roots | recurse children | get components").Ok);
            Assert.IsNull(_box.sharedMaterial, "a physics material instance was created by a read");
            StringAssert.Contains("explicit-only", RqlUnitySchema.Render(RqlUnity.Engine, "BoxCollider"));
        }

        [Test]
        public void ColliderMembersAreWritable()
        {
            var r = RqlUnity.Engine.Execute("objects | get components | where $ is BoxCollider | set isTrigger = true, size = \"(2, 2, 2)\"",
                null, new QueryOptions { AllowMutation = true });
            Assert.IsTrue(r.Ok, Table(r));
            Assert.IsTrue(_box.isTrigger);
            Assert.AreEqual(Vector3.one * 2, _box.size);
        }
    }
}
