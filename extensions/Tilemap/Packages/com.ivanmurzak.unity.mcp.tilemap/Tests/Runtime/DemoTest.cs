/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Tilemap) │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the MIT License.                                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Tilemap.Runtime.Tests
{
    public partial class DemoTest
    {
        [UnityTest]
        public IEnumerator Always_Valid_Test()
        {
            Assert.IsTrue(true, "Runtime placeholder test.");
            yield return null;
        }
    }
}
