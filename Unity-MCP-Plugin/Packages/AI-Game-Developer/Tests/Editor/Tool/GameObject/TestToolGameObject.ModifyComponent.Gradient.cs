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
using System;
using System.Collections;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        [UnityTest]
        public IEnumerator ModifyComponent_Gradient_SimpleBlend()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var go = new GameObject(GO_ParentName);
            var component = go.AddComponent<GradientTest>();

            var expectedGradient = new Gradient();
            expectedGradient.mode = GradientMode.Blend;
            expectedGradient.SetKeys(
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

            var componentDiff = SerializedMember.FromValue(
                    reflector: reflector,
                    name: null,
                    type: typeof(GradientTest),
                    value: new ComponentRef(component.GetInstanceID()))
                .AddField(SerializedMember.FromValue(
                    reflector: reflector,
                    name: nameof(GradientTest.gradient),
                    value: expectedGradient));

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef(component.GetInstanceID()),
                componentDiff: componentDiff);

            Assert.IsTrue(response.Success, $"Modification should be successful. Logs:\n{string.Join("\n", response.Logs ?? Array.Empty<string>())}");

            Assert.AreEqual(expectedGradient.colorKeys.Length, component.gradient.colorKeys.Length, "ColorKeys length should match");
            Assert.AreEqual(expectedGradient.alphaKeys.Length, component.gradient.alphaKeys.Length, "AlphaKeys length should match");
            Assert.AreEqual(expectedGradient.mode, component.gradient.mode, "GradientMode should match");

            for (int i = 0; i < expectedGradient.colorKeys.Length; i++)
            {
                Assert.AreEqual(expectedGradient.colorKeys[i].time, component.gradient.colorKeys[i].time, 0.001f, $"ColorKey[{i}].time should match");
                Assert.AreEqual(expectedGradient.colorKeys[i].color.r, component.gradient.colorKeys[i].color.r, 0.001f, $"ColorKey[{i}].color.r should match");
                Assert.AreEqual(expectedGradient.colorKeys[i].color.g, component.gradient.colorKeys[i].color.g, 0.001f, $"ColorKey[{i}].color.g should match");
                Assert.AreEqual(expectedGradient.colorKeys[i].color.b, component.gradient.colorKeys[i].color.b, 0.001f, $"ColorKey[{i}].color.b should match");
            }

            for (int i = 0; i < expectedGradient.alphaKeys.Length; i++)
            {
                Assert.AreEqual(expectedGradient.alphaKeys[i].time, component.gradient.alphaKeys[i].time, 0.001f, $"AlphaKey[{i}].time should match");
                Assert.AreEqual(expectedGradient.alphaKeys[i].alpha, component.gradient.alphaKeys[i].alpha, 0.001f, $"AlphaKey[{i}].alpha should match");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ModifyComponent_Gradient_Rainbow()
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");

            var go = new GameObject(GO_ParentName);
            var component = go.AddComponent<GradientTest>();

            var expectedGradient = new Gradient();
            expectedGradient.mode = GradientMode.Blend;
            expectedGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.17f),
                    new GradientColorKey(Color.yellow, 0.33f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.blue, 0.67f),
                    new GradientColorKey(new Color(0.29f, 0f, 0.51f), 0.83f),
                    new GradientColorKey(new Color(0.56f, 0f, 1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.1f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0.5f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            var componentDiff = SerializedMember.FromValue(
                    reflector: reflector,
                    name: null,
                    type: typeof(GradientTest),
                    value: new ComponentRef(component.GetInstanceID()))
                .AddField(SerializedMember.FromValue(
                    reflector: reflector,
                    name: nameof(GradientTest.gradient),
                    value: expectedGradient));

            var response = new Tool_GameObject().ModifyComponent(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                componentRef: new ComponentRef(component.GetInstanceID()),
                componentDiff: componentDiff);

            Assert.IsTrue(response.Success, $"Modification should be successful. Logs:\n{string.Join("\n", response.Logs ?? Array.Empty<string>())}");

            Assert.AreEqual(expectedGradient.colorKeys.Length, component.gradient.colorKeys.Length, "ColorKeys length should match");
            Assert.AreEqual(expectedGradient.alphaKeys.Length, component.gradient.alphaKeys.Length, "AlphaKeys length should match");
            Assert.AreEqual(expectedGradient.mode, component.gradient.mode, "GradientMode should match");

            yield return null;
        }
    }
}
