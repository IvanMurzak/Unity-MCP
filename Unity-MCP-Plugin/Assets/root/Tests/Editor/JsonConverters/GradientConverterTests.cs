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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.JsonConverter
{
    using com.IvanMurzak.Unity.MCP.Editor.Tests;

    public class GradientConverterTests : BaseTest
    {
        #region GradientColorKey

        [UnityTest]
        public IEnumerator GradientColorKey_Default()
        {
            TestUtils.ValidateType(new GradientColorKey(Color.white, 0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GradientColorKey_Custom()
        {
            TestUtils.ValidateType(new GradientColorKey(new Color(0.5f, 0.3f, 0.8f, 1f), 0.75f));
            yield return null;
        }

        #endregion

        #region GradientAlphaKey

        [UnityTest]
        public IEnumerator GradientAlphaKey_Default()
        {
            TestUtils.ValidateType(new GradientAlphaKey(1f, 0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GradientAlphaKey_Custom()
        {
            TestUtils.ValidateType(new GradientAlphaKey(0.5f, 0.75f));
            yield return null;
        }

        #endregion

        #region Gradient

        [UnityTest]
        public IEnumerator Gradient_Default()
        {
            TestUtils.ValidateType(new Gradient());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gradient_SimpleBlend()
        {
            var gradient = new Gradient();
            gradient.mode = GradientMode.Blend;
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(Color.blue, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            TestUtils.ValidateType(gradient);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gradient_MultipleKeys()
        {
            var gradient = new Gradient();
            gradient.mode = GradientMode.Blend;
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.75f, 0.35f, 1f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f, 1f), 0.6f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0.02f, 1f), 0.85f),
                    new GradientColorKey(new Color(0.15f, 0.03f, 0.005f, 1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.3f, 0.05f),
                    new GradientAlphaKey(0.7f, 0.12f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0.25f, 0.88f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            TestUtils.ValidateType(gradient);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gradient_FixedMode()
        {
            var gradient = new Gradient();
            gradient.mode = GradientMode.Fixed;
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.green, 0f),
                    new GradientColorKey(Color.yellow, 0.5f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            TestUtils.ValidateType(gradient);
            yield return null;
        }

        #endregion
    }
}
