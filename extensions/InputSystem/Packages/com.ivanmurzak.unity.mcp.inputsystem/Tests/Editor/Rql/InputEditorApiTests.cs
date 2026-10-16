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
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;

    public class InputEditorApiTests : InputSystemTestBase
    {
        [Test]
        public void CreateAssetWritesAnInputActionsFileWithTheFirstMap()
        {
            const string path = Folder + "/New/Created.inputactions";
            var r = Ok($"input | call CreateAsset(\"{path}\", \"Gameplay\")");
            StringAssert.Contains("Created", Table(r));
            Assert.IsTrue(File.Exists(path));
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            Assert.IsNotNull(asset);
            Assert.AreEqual(1, asset.actionMaps.Count);
            Assert.AreEqual("Gameplay", asset.actionMaps[0].name);
            using (var saved = FromFile(path)) Assert.AreEqual("Gameplay", saved.Asset.actionMaps[0].name);
        }

        [Test]
        public void CreateAssetWithoutAMapIsEmptyButValid()
        {
            const string path = Folder + "/Empty.inputactions";
            Ok($"input | call CreateAsset(\"{path}\")");
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            Assert.IsNotNull(asset);
            Assert.AreEqual(0, asset.actionMaps.Count);
            Ok($"unity.asset(\"{path}\") | call AddMap(\"Player\")");
            using (var saved = FromFile(path)) Assert.AreEqual("Player", saved.Asset.actionMaps[0].name);
        }

        [Test]
        public void ControlSchemesPersistInAnAssetWithoutMaps()
        {
            const string path = Folder + "/Schemes.inputactions";
            Ok($"input | call CreateAsset(\"{path}\")");
            Ok($"unity.asset(\"{path}\") | call AddControlScheme(\"Gamepad\", [\"<Gamepad>\"])");
            using (var saved = FromFile(path)) Assert.AreEqual("Gamepad", saved.Asset.controlSchemes.Single().name);

            Ok(A + " | get maps | call Remove()");
            using (var saved = FromFile())
            {
                Assert.AreEqual(0, saved.Asset.actionMaps.Count);
                Assert.AreEqual(2, saved.Asset.controlSchemes.Count);
            }
        }

        [Test]
        public void CreateAssetValidatesThePath()
        {
            Fails("input | call CreateAsset(\"Player.inputactions\")", "must start with Assets/");
            Fails($"input | call CreateAsset(\"{Folder}/Player.asset\")", "must end with .inputactions");
            Fails($"input | call CreateAsset(\"{P}\")", "already exists");
            Fails($"input | call CreateAsset(\"{Folder}/Bad.inputactions\", \"A/B\")", "must not contain '/'");
            Assert.IsFalse(File.Exists(Folder + "/Bad.inputactions"));
        }

        [Test]
        public void AssetsListsTheProjectsInputActionAssets()
        {
            var r = Read("input | get assets | where path == \"" + P + "\" | select name path");
            Assert.AreEqual(1, r.Rows!.Count, Table(r));
            Assert.AreEqual("Fixture", Cell(r));
        }

        [Test]
        public void RegisteredNamesAreReadable()
        {
            var r = Read("input | select interactions processors");
            StringAssert.Contains("Hold", Table(r));
            StringAssert.Contains("Invert", Table(r));
            var layouts = Read("input | get layouts | where $ == \"Gamepad\"");
            Assert.AreEqual("1 result\nGamepad", Table(layouts).Trim());
        }
    }
}
