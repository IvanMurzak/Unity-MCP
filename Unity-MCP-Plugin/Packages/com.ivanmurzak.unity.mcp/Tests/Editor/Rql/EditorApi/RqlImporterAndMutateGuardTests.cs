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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>An asset's import settings through <c>importer</c>, and what rql-mutate reports that changed nothing.</summary>
    public class RqlImporterAndMutateGuardTests : EditorApiTestBase
    {
        private string MakeTexture()
        {
            var path = $"{Folder}/grass.png";
            var texture = new Texture2D(8, 8);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            // A project in 2D mode imports new textures as sprites: start from Default whatever the project's mode.
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                importer.SaveAndReimport();
            }
            return path;
        }

        [Test]
        public void AnAssetReadsItsImporter()
        {
            var path = MakeTexture();
            Assert.AreEqual("TextureImporter", Cell(Q($"unity.asset(\"{path}\") | get importer | select $type")));
            Assert.AreEqual("Default", Cell(Q($"unity.asset(\"{path}\") | get importer | select textureType")));
            new GameObject("SceneObject");
            Assert.AreEqual("null", Cell(Q("objects | where name == \"SceneObject\" | select importer")));
            StringAssert.Contains("importer AssetImporter ro explicit-only", Schema("Texture2D"));
            StringAssert.DoesNotContain("importer", Schema("Light")); // a component is never an asset file
        }

        // Import settings take effect on reimport: rql-mutate saves the importer and reimports the file.
        [Test]
        public void ImportSettingsWrittenByRqlMutateAreReimported()
        {
            var path = MakeTexture();
            Assert.IsFalse(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any());
            // spriteImportMode too, as the Inspector does: on 6000.0 the texture had no sprite after textureType alone
            AssertOk(RqlUnity.Mutate($"unity.asset(\"{path}\") | get importer | set textureType = \"Sprite\", spriteImportMode = \"Single\""));
            Assert.AreEqual(TextureImporterType.Sprite, ((TextureImporter)AssetImporter.GetAtPath(path)).textureType);
            Assert.IsTrue(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any(), "the texture was not reimported as a sprite");
            StringAssert.Contains("textureType: 8", File.ReadAllText(path + ".meta"));
            Assert.AreEqual(0, RqlUnity.Reimports.Count);
        }

        [Test]
        public void ADryRunLeavesTheImportSettingsAlone()
        {
            var path = MakeTexture();
            AssertOk(RqlUnity.Mutate($"unity.asset(\"{path}\") | get importer | set textureType = \"Sprite\"", dryRun: true));
            Assert.AreEqual(TextureImporterType.Default, ((TextureImporter)AssetImporter.GetAtPath(path)).textureType);
            Assert.AreEqual(0, RqlUnity.Reimports.Count);
        }

        // "0 results" plus a warning read as success: a misspelt member that leaves nothing to change fails the call.
        [Test]
        public void AMisspeltMemberThatMatchesNothingFailsTheMutation()
        {
            new GameObject("Target");
            var typo = RqlUnity.Mutate("objects | where name == \"Target\" | get transfrom | set position = \"(1, 2, 3)\"");
            AssertFails(typo, "unknown-member", "nothing was changed");
            StringAssert.Contains("no member 'transfrom'", Table(typo));
            // An empty where without a misspelling is still a successful no-op.
            AssertOk(RqlUnity.Mutate("objects | where name == \"Nobody\" | set name = \"X\""));
            // A real member that no item of a mixed stream has is a successful no-op.
            AssertOk(RqlUnity.Mutate("objects | get components | where intensity > 1 | set intensity = 2"));
            // A dry run is checked the same way.
            AssertFails(RqlUnity.Mutate("objects | where name == \"Target\" | get transfrom | set position = \"(1, 2, 3)\"", dryRun: true), "unknown-member");
        }

        [Test]
        public void ThePlayModeNoticeIsOnlyGivenInPlayMode()
        {
            Assert.IsNull(RqlUnity.PlayModeNotice(isPlaying: false));
            var notice = RqlUnity.PlayModeNotice(isPlaying: true)!;
            Assert.AreEqual("play-mode", notice.Code);
            StringAssert.Contains("lost when Play Mode exits", notice.Message);
            // an Edit Mode mutation carries no such warning
            new GameObject("Target");
            Assert.IsFalse(Warns(RqlUnity.Mutate("objects | where name == \"Target\" | set name = \"Renamed\""), "play-mode"));
        }
    }
}
