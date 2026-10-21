/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    using CinemachineCamera = global::Unity.Cinemachine.CinemachineCamera;

    /// <summary>A fresh empty scene per test (no Camera) with a fresh RQL engine; helpers to create cameras and targets.</summary>
    public abstract class CinemachineTestBase
    {
        protected const string Cameras = "cinemachine | get cameras";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
        }

        /// <summary>A GameObject with a CinemachineCamera named <paramref name="name"/>.</summary>
        protected static CinemachineCamera CreateCamera(string name = "Cam") => new GameObject(name).AddComponent<CinemachineCamera>();

        protected static GameObject CreateTarget(string name, Vector3 position = default)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            return go;
        }

        /// <summary>Every component of type T in the loaded scenes, inactive ones included.</summary>
        protected static List<T> All<T>() where T : Component =>
            RqlUnity.AllGameObjects().SelectMany(g => g.GetComponents<T>()).Where(c => c != null).ToList();

        /// <summary>The $ref of a GameObject (for ref("…") in queries).</summary>
        protected static string Ref(GameObject go) =>
            Cell(Q("objects | where name == \"" + go.name + "\" | select $ref"));

        /// <summary>The camera query narrowed to one camera by name.</summary>
        protected static string Cam(string name = "Cam") => Cameras + " | where name == \"" + name + "\"";

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        protected static QueryResult Ok(QueryResult r)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        protected static QueryResult Fails(QueryResult r)
        {
            Assert.IsFalse(r.Ok, Table(r));
            return r;
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0)
        {
            Ok(r);
            return r.Rows![0][column]?.ToJsonString().Trim('"') ?? "null";
        }
    }
}
