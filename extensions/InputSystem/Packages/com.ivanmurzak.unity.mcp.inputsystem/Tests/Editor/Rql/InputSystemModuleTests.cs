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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem;
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputActionType = UnityEngine.InputSystem.InputActionType;

    public class InputSystemModuleTests : InputSystemTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(InputSystemModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = Guide();
            StringAssert.Contains("## " + InputSystemModule.GuideTopic, skill);
            foreach (var op in new[] { "input | call CreateAsset(", "call AddMap(", "call AddAction(", "call AddBinding(", "call AddComposite(",
                                       "call AddControlScheme(", "call Remove()" })
                StringAssert.Contains(op, skill);
        }

        [Test]
        public void TheSchemaListsTheSourceAndTheMethods()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("input", overview);
            var asset = RqlUnitySchema.Render(RqlUnity.Engine, "InputActionAsset");
            StringAssert.Contains("AddMap(", asset);
            StringAssert.Contains("AddControlScheme(", asset);
            var action = Table(Read(A + " | get actions | first 1 | describe"));
            foreach (var member in new[] { "AddBinding", "AddComposite", "expectedControlType", "interactions" })
                StringAssert.Contains(member, action);
        }

        /// <summary>Every example query of the guide runs, in order, on a project without the example asset.</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            const string path = Folder + "/Guide/Player.inputactions";
            var block = Regex.Match(Guide(), "```\\n(.*?)```", RegexOptions.Singleline).Groups[1].Value;
            var queries = block.Split('\n')
                .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())
                .Where(l => l.Length > 0)
                .Select(l => l.Replace(InputSystemModule.ExamplePath, path))
                .ToList();
            Assert.GreaterOrEqual(queries.Count, 25);
            foreach (var q in queries)
            {
                var r = M(q);
                Assert.IsTrue(r.Ok, q + "\n" + Table(r));
            }

            using var saved = FromFile(path);
            var asset = saved.Asset;
            Assert.AreEqual("Gameplay", asset.actionMaps.Single().name);
            Assert.AreEqual("Keyboard&Mouse", asset.controlSchemes.Single().name);
            CollectionAssert.AreEqual(new[] { "<Keyboard>" },
                asset.controlSchemes[0].deviceRequirements.Where(d => !d.isOptional).Select(d => d.controlPath).ToArray());
            var move = asset.FindAction("Gameplay/Move", true);
            Assert.AreEqual(InputActionType.Value, move.type);
            Assert.AreEqual("invertVector2", move.processors);
            Assert.AreEqual(5, move.bindings.Count);
            Assert.AreEqual("<Keyboard>/upArrow", move.bindings[1].path);
            Assert.IsNull(asset.FindAction("Gameplay/Leap"));
        }

        [Test]
        public void EditsPersistAfterReimport()
        {
            Ok(Action("Jump") + " | set name = \"Leap\", interactions = \"tap\"");
            Ok(Action("Leap") + " | call AddBinding(\"<Gamepad>/rightTrigger\", \"Gamepad\")");
            Ok(Scheme("Gamepad") + " | set optionalDevices = [\"<Joystick>\"]");

            AssetDatabase.ImportAsset(P, ImportAssetOptions.ForceUpdate);
            RqlUnity.Reset();
            var leap = Asset.FindAction("Player/Leap", true);
            Assert.AreEqual("tap", leap.interactions);
            Assert.AreEqual("<Gamepad>/rightTrigger", leap.bindings.Last().path);
            var r = Read(Scheme("Gamepad") + " | select optionalDevices");
            StringAssert.Contains("<Joystick>", Cell(r));
            var bindings = Read(Action("Leap") + " | get bindings | select path");
            Assert.AreEqual(3, bindings.Rows!.Count, Table(bindings));
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var query = Map("Player") + " | call AddAction(\"Crouch\", \"Button\", null, \"<Keyboard>/c\")";
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(query)).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            using var saved = FromFile();
            Assert.AreEqual("<Keyboard>/c", saved.Asset.FindAction("Player/Crouch", true).bindings[0].path);
        }

        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## " + InputSystemModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "the Input System guide is not in the unity-rql skill");
            return skill.Substring(start);
        }
    }
}
