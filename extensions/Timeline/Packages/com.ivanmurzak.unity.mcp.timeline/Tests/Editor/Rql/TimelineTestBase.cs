/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;
    using TrackAsset = UnityEngine.Timeline.TrackAsset;

    /// <summary>
    /// A fresh empty scene and a fresh RQL engine per test. <see cref="Timeline"/> is an empty TimelineAsset saved at
    /// <see cref="TimelinePath"/>; everything a test creates goes to <see cref="Folder"/>, which is deleted afterwards.
    /// </summary>
    public abstract class TimelineTestBase
    {
        protected const string Folder = "Assets/RqlTimelineTests";
        protected const string TimelinePath = Folder + "/Intro.playable";
        protected const string A = "unity.asset(\"" + TimelinePath + "\")";
        protected const string D = "objects | get components | where $ is PlayableDirector";

        protected TimelineAsset Timeline = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            EnsureFolder();
            Timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(Timeline, TimelinePath);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A query that must succeed.</summary>
        protected static QueryResult Read(string rql)
        {
            var r = Q(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql)
        {
            var r = M(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>.</summary>
        protected static void Fails(string rql, string expected)
        {
            var r = M(rql);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0, int row = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r.Rows![row][column]?.ToJsonString().Trim('"') ?? "null";
        }

        /// <summary>The <c>result</c> column of a call receipt.</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlTimelineTests");
        }

        protected TrackAsset[] Tracks => Timeline.GetRootTracks().Where(t => t != Timeline.markerTrack).ToArray();

        protected T AddTrack<T>(string name, TrackAsset? parent = null) where T : TrackAsset, new() =>
            (T)Timeline.CreateTrack(typeof(T), parent, name);

        protected static AnimationClip AnimationAsset(string name = "Walk", float length = 2f)
        {
            EnsureFolder();
            var clip = new AnimationClip { name = name };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, length, 1f));
            AssetDatabase.CreateAsset(clip, $"{Folder}/{name}.anim");
            return clip;
        }

        protected static SignalAsset SignalAssetAt(string name = "Boom")
        {
            EnsureFolder();
            var signal = ScriptableObject.CreateInstance<SignalAsset>();
            AssetDatabase.CreateAsset(signal, $"{Folder}/{name}.signal");
            return signal;
        }
    }
}
