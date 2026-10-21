/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;
    using AnimatorState = UnityEditor.Animations.AnimatorState;
    using AnimatorStateMachine = UnityEditor.Animations.AnimatorStateMachine;

    /// <summary>
    /// A fresh empty scene per test and two assets in <see cref="Folder"/> (deleted afterwards): the clip "Walk" (curve
    /// Transform.localPosition.y with keys (0, 0) (1, 1)) and the controller "Player" (parameter Speed: Float, states Idle (default)
    /// and Walk on the Base Layer).
    /// </summary>
    public abstract class AnimationTestBase
    {
        protected const string Folder = "Assets/RqlAnimationTests";
        protected const string ClipPath = Folder + "/Walk.anim";
        protected const string ControllerPath = Folder + "/Player.controller";

        /// <summary>The clip as an RQL source.</summary>
        protected const string W = "unity.asset(\"" + ClipPath + "\")";

        /// <summary>The controller as an RQL source.</summary>
        protected const string P = "unity.asset(\"" + ControllerPath + "\")";

        protected AnimationClip Clip = null!;
        protected AnimatorController Controller = null!;

        protected AnimatorStateMachine Base => Controller.layers[0].stateMachine;

        protected AnimatorState StateNamed(string name, int layer = 0) =>
            Controller.layers[layer].stateMachine.states.Select(s => s.state).First(s => s.name == name);

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            EnsureFolder();

            Clip = new AnimationClip { name = "Walk" };
            AnimationUtility.SetEditorCurve(Clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.y"),
                new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f)));
            AssetDatabase.CreateAsset(Clip, ClipPath);

            Controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            Controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var idle = Controller.layers[0].stateMachine.AddState("Idle");
            Controller.layers[0].stateMachine.AddState("Walk").motion = Clip;
            Controller.layers[0].stateMachine.defaultState = idle;
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlAnimationTests");
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A query that must succeed.</summary>
        protected static QueryResult Ok(QueryResult r)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql) => Ok(M(rql));

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>.</summary>
        protected static void Fails(string rql, string expected)
        {
            var r = M(rql);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static long Count(string rql) => Ok(Q(rql)).Value!.GetValue<long>();

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0)
        {
            Ok(r);
            return r.Rows![0][column]!.ToJsonString().Trim('"');
        }

        /// <summary>The <c>result</c> column of a call receipt ($ref | method | result).</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static AnimationCurve CurveOf(AnimationClip clip, string property, string path = "") =>
            AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
    }
}
