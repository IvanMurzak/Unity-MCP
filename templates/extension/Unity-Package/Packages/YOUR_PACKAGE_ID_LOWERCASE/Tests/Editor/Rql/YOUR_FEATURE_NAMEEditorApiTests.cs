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
using com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME;
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.YOUR_FEATURE_NAME
{
    using GameObject = UnityEngine.GameObject;
    using Light = UnityEngine.Light;
    using LightType = UnityEngine.LightType;
    using Vector3 = UnityEngine.Vector3;

    /// <summary>The example source: <c>YOUR_FEATURE_NAME_LOWERCASE | call CreateLight(…)</c>.</summary>
    public class YOUR_FEATURE_NAMEEditorApiTests : YOUR_FEATURE_NAMETestBase
    {
        private const string S = YOUR_FEATURE_NAMEModule.SourceName;

        [Test]
        public void CreateLightMakesAnUndoableLight()
        {
            var r = Ok(S + " | call CreateLight(\"Lamp\", \"Spot\", \"(1, 2, 3)\")");
            StringAssert.Contains("GameObject#", Result(r));

            var go = GameObject.Find("Lamp");
            Assert.IsNotNull(go);
            Assert.AreEqual(LightType.Spot, go!.GetComponent<Light>().type);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.position);

            Undo.PerformUndo();
            Assert.IsNull(GameObject.Find("Lamp"));
        }

        [Test]
        public void CreateLightUsesDefaults()
        {
            Ok(S + " | call CreateLight()");
            var go = GameObject.Find("Light");
            Assert.IsNotNull(go);
            Assert.AreEqual(LightType.Point, go!.GetComponent<Light>().type);
        }

        [Test]
        public void CreateLightRejectsAnEmptyName()
        {
            Fails(S + " | call CreateLight(\"  \")", "name is empty");
            Assert.AreEqual(1, Q(L + " | select gameObject.name").Rows!.Count, "no light was created");
        }

        [Test]
        public void TheSourceIsListedInTheSchema() => StringAssert.Contains(S, Schema(null));
    }
}
