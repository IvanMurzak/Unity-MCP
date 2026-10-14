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
