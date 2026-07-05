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
