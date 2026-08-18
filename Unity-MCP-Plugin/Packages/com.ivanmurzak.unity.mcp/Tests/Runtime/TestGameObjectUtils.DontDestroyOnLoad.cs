/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Runtime.Tests
{
    public class TestGameObjectUtilsDontDestroyOnLoad
    {
        [UnityTest]
        public IEnumerator FindByName_DontDestroyOnLoad_ObjectIsFound()
        {
            // DontDestroyOnLoad only exists once the Editor/Player is actually in Play
            // Mode. This test must run under the PlayMode test category (not EditMode).
            Assert.IsTrue(Application.isPlaying, "This test must run in Play Mode.");

            // GameObjectUtils.FindRootGameObjects(scene: null) is only supported inside
            // the Editor (see GameObjectUtils.Runtime*.cs) — a standalone Player build
            // (this test assembly's "standalone" PlayMode CI run) always returns null
            // there, by design, since DDOL-in-a-standalone-build lookup is out of scope
            // for this Editor-bridge tool. Skip rather than fail in that environment.
            if (!Application.isEditor)
            {
                Assert.Ignore("FindByName(scene: null) is Editor-only; skipping in a standalone Player build.");
                yield break;
            }

            var objectName = "ddolTestObject_" + System.Guid.NewGuid().ToString("N");
            var go = new GameObject(objectName);
            Object.DontDestroyOnLoad(go);

            try
            {
                Assert.IsNotNull(GameObjectUtils.FindByName(objectName),
                    $"'{objectName}' lives in the DontDestroyOnLoad scene and should still be found");
            }
            finally
            {
                Object.Destroy(go);
            }

            yield return null;
        }
    }
}
