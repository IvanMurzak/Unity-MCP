/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.IO;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputActionType = UnityEngine.InputSystem.InputActionType;
    using InputControlScheme = UnityEngine.InputSystem.InputControlScheme;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// A fresh scene and a fixture asset <see cref="P"/> per test: map "Player" with "Jump" (Button: &lt;Keyboard&gt;/space
    /// in Keyboard&amp;Mouse, &lt;Gamepad&gt;/buttonSouth in Gamepad) and "Move" (Value Vector2: a WASD 2DVector composite),
    /// map "UI" with "Click", schemes "Keyboard&amp;Mouse" (Keyboard required, Mouse optional) and "Gamepad". Everything a test
    /// creates lives in <see cref="Folder"/>, which is deleted afterwards.
    /// </summary>
    public abstract class InputSystemTestBase
    {
        protected const string Folder = "Assets/RqlInputSystemTests";
        protected const string P = Folder + "/Fixture.inputactions";
        protected const string A = "unity.asset(\"" + P + "\")";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            EnsureFolder();
            WriteFixture(P);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlInputSystemTests");
        }

        protected static void WriteFixture(string path)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                asset.name = Path.GetFileNameWithoutExtension(path);
                Setup.AddControlScheme(asset, new InputControlScheme("Keyboard&Mouse", new[]
                {
                    new InputControlScheme.DeviceRequirement { controlPath = "<Keyboard>" },
                    new InputControlScheme.DeviceRequirement { controlPath = "<Mouse>", isOptional = true },
                }, "Keyboard&Mouse"));
                Setup.AddControlScheme(asset, new InputControlScheme("Gamepad", new[]
                {
                    new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>" },
                }, "Gamepad"));
                var player = Setup.AddActionMap(asset, "Player");
                var jump = Setup.AddAction(player, "Jump", InputActionType.Button);
                Setup.AddBinding(jump, "<Keyboard>/space", groups: "Keyboard&Mouse");
                Setup.AddBinding(jump, "<Gamepad>/buttonSouth", groups: "Gamepad");
                var move = Setup.AddAction(player, "Move", InputActionType.Value);
                move.expectedControlType = "Vector2";
                Setup.AddCompositeBinding(move, "2DVector")
                    .With("up", "<Keyboard>/w", "Keyboard&Mouse")
                    .With("down", "<Keyboard>/s", "Keyboard&Mouse")
                    .With("left", "<Keyboard>/a", "Keyboard&Mouse")
                    .With("right", "<Keyboard>/d", "Keyboard&Mouse");
                var ui = Setup.AddActionMap(asset, "UI");
                Setup.AddAction(ui, "Click", InputActionType.Button, "<Mouse>/leftButton", groups: "Keyboard&Mouse");
                File.WriteAllText(path, asset.ToJson());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>The imported asset.</summary>
        protected static InputActionAsset Asset => AssetDatabase.LoadAssetAtPath<InputActionAsset>(P);

        /// <summary>The asset as stored in the file (proves a change was persisted); the caller's using block destroys it.</summary>
        protected static Saved FromFile(string path = P) => new Saved(InputActionAsset.FromJson(File.ReadAllText(path)));

        protected sealed class Saved : IDisposable
        {
            public Saved(InputActionAsset asset) { Asset = asset; }
            public InputActionAsset Asset { get; }
            public void Dispose() => UnityEngine.Object.DestroyImmediate(Asset);
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        // Through RqlUnity.Mutate, as the rql-mutate tool runs it: file writes are deferred to the end of the query.
        protected static QueryResult M(string rql) => RqlUnity.Mutate(rql);

        /// <summary>A query that must succeed.</summary>
        protected static QueryResult Read(string rql)
        {
            var r = Q(rql);
            Assert.IsTrue(r.Ok, rql + "\n" + Table(r));
            return r;
        }

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql)
        {
            var r = M(rql);
            Assert.IsTrue(r.Ok, rql + "\n" + Table(r));
            return r;
        }

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>; the file must stay unchanged.</summary>
        protected static void Fails(string rql, string expected)
        {
            var before = File.ReadAllText(P);
            var r = M(rql);
            Assert.IsFalse(r.Ok, rql + "\n" + Table(r));
            StringAssert.Contains(expected, Table(r));
            Assert.AreEqual(before, File.ReadAllText(P), "a failed mutation changed the file");
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0, int row = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            var node = r.Rows![row][column];
            if (node is System.Text.Json.Nodes.JsonValue value && value.TryGetValue<string>(out var text)) return text;
            // JSON escapes <, > and & in nested strings; tests compare the readable text.
            return node?.ToJsonString().Replace("\\u003C", "<").Replace("\\u003E", ">").Replace("\\u0026", "&") ?? "null";
        }

        /// <summary>The <c>result</c> column of a call receipt.</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static string Action(string name) => A + $" | get actions | where name == \"{name}\"";

        protected static string Map(string name) => A + $" | get maps | where name == \"{name}\"";

        protected static string Scheme(string name) => A + $" | get controlSchemes | where name == \"{name}\"";

        protected static string[] Paths(InputActionAsset asset, string action) =>
            asset.FindAction(action, true).bindings.Select(b => b.path).ToArray();
    }
}
