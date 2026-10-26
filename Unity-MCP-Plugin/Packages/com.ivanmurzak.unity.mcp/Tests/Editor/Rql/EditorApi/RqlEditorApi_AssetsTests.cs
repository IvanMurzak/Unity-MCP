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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    public class RqlEditorApi_AssetsTests : EditorApiTestBase
    {
        [Test]
        public void CreateMaterialInANewFolderThenQueryIt()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/Mats/Glow.mat\")"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Mats/Glow.mat"));
            Assert.AreEqual("Glow", Cell(Q($"unity.assets(\"t:Material\", \"{Folder}\") | select name")));
        }

        [Test]
        public void CopyMoveAndDelete()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/A.mat\")"));
            AssertOk(M($"editor | call CopyAsset(\"{Folder}/A.mat\", \"{Folder}/B.mat\")"));
            AssertOk(M($"editor | call MoveAsset(\"{Folder}/B.mat\", \"{Folder}/Sub/C.mat\")"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Sub/C.mat"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/B.mat"));
            AssertOk(M($"editor | call DeleteAsset(\"{Folder}/A.mat\")"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/A.mat"));
        }

        [Test]
        public void FailuresAreReported()
        {
            Assert.IsFalse(M("editor | call MoveAsset(\"Assets/NoSuch.mat\", \"Assets/X.mat\")").Ok);
            Assert.IsFalse(M("editor | call CreateMaterial(\"Assets/RqlEditorApiTests/X.mat\", \"No/Such/Shader\")").Ok);
        }

        [Test]
        public void AssetsAreAssignableByPath()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/ByPath.mat\")"));
            var renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            AssertOk(M($"objects | get components | where $ is MeshRenderer | set sharedMaterial = \"{Folder}/ByPath.mat\""));
            Assert.AreEqual("ByPath", renderer.sharedMaterial.name);
            AssertFails(M("objects | get components | where $ is MeshRenderer | set sharedMaterial = \"Assets/NoSuch.mat\""), "coercion-failed", "no asset at");
            AssertFails(M($"objects | get components | where $ is MeshRenderer | set sharedMaterial = \"{Folder}\""), "coercion-failed", "expected Material");
        }

        [Test]
        public void SpritesFromTexturesAndPaths()
        {
            var path = $"{Folder}/dot.png";
            var tex = new Texture2D(4, 4);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; // not the default on every Unity version
            importer.SaveAndReimport();

            var sr = new GameObject("S").AddComponent<SpriteRenderer>();
            AssertOk(M($"objects | get components | where $ is SpriteRenderer | set sprite = \"{path}\""));
            StringAssert.StartsWith("dot", sr.sprite.name);   // "dot" or "dot_0" depending on the sprite import mode
            sr.sprite = null;
            AssertOk(M($"objects | get components | where $ is SpriteRenderer | set sprite = unity.asset(\"{path}\")"));
            Assert.IsNotNull(sr.sprite);
            AssertFails(M($"objects | get components | where $ is SpriteRenderer | set sprite = \"{path}#nope\""), "coercion-failed", "has no sprite 'nope'");
        }

        // Unity's CreateAsset / CopyAsset replace an existing asset silently: references to it break.
        [Test]
        public void CreateAndCopyNeverReplaceAnExistingAsset()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/A.mat\")"));
            var renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/A.mat");
            AssertFails(M($"editor | call CreateMaterial(\"{Folder}/A.mat\")"), "getter-threw", "already exists");
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/B.mat\")"));
            AssertFails(M($"editor | call CopyAsset(\"{Folder}/B.mat\", \"{Folder}/A.mat\")"), "getter-threw", "already exists");
            Assert.IsNotNull(renderer.sharedMaterial, "the existing material (and the reference to it) must survive");
            AssertFails(M($"editor | call CreateMaterial(\"{Folder}/C.txt\")"), "getter-threw", "must end with .mat");
            AssertFails(M($"editor | call CopyAsset(\"{Folder}/B.mat\", \"{Folder}/C.txt\")"), "getter-threw", "must end with .mat");
        }

        [Test]
        public void DeletingAnAssetInUseNeedsForce()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/Used.mat\")"));
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "UsesIt";
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Used.mat");
            AssertFails(M($"editor | call DeleteAsset(\"{Folder}/Used.mat\")"), "getter-threw", "used by 1 object(s) in the open scenes: UsesIt");
            AssertFails(M($"editor | call DeleteAsset(\"{Folder}\")"), "getter-threw", "UsesIt"); // a folder holding it, too
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Used.mat"));
            AssertOk(M($"editor | call DeleteAsset(\"{Folder}/Used.mat\", true)"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Used.mat"));
        }

        [Test]
        public void AFolderWithADotInItsNameIsCopied()
        {
            AssertOk(M($"editor | call CreateFolder(\"{Folder}/com.foo\")"));
            AssertOk(M($"editor | call CopyAsset(\"{Folder}/com.foo\", \"{Folder}/Copy\")"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/Copy"));
        }

        [Test]
        public void FolderPathsAreNormalized()
        {
            // The RQL string "Assets\\RqlEditorApiTests\\N\\M\\" is Assets\RqlEditorApiTests\N\M\ (backslashes, a trailing one).
            Assert.AreEqual($"{Folder}/N/M", Cell(M("editor | call CreateFolder(\"Assets\\\\RqlEditorApiTests\\\\N\\\\M\\\\\")"), 0, 2));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/N/M"));
        }

        [Test]
        public void AssetsInAMissingFolderIsAnError()
        {
            AssertFails(Q("unity.assets(\"t:Material\", \"Assets/NoSuchFolder\") | count"), "not-found", "no folder 'Assets/NoSuchFolder'");
        }

        [Test]
        public void CreateFolderMakesParents()
        {
            AssertOk(M($"editor | call CreateFolder(\"{Folder}/a/b/c\")"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{Folder}/a/b/c"));
        }

        [Test]
        public void AssetEditsAreDirtyAndSaveable()
        {
            AssertOk(M($"editor | call CreateMaterial(\"{Folder}/M.mat\")"));
            AssertOk(M($"unity.asset(\"{Folder}/M.mat\") | set renderQueue = 3000"));
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/M.mat");
            Assert.AreEqual(3000, mat.renderQueue);
            Assert.IsTrue(EditorUtility.IsDirty(mat));
            AssertOk(M("editor | call SaveAssets()"));
            Assert.IsFalse(EditorUtility.IsDirty(mat));
        }
    }
}
