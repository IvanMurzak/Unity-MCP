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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolReflection : BaseTest
    {
        void ResultValidation(string result)
        {
            UnityEngine.Debug.Log(result);
            Assert.IsFalse(result.Contains("[Error]"), $"[Error] {result}");
            Assert.IsTrue(result.Contains("[Success]"), $"[Success] {result}");
        }
        [UnityTest]
        public IEnumerator MethodFind_Transform()
        {
            var paramTypes = new[]
            {
                typeof(UnityEngine.Transform),
                typeof(UnityEngine.Vector3)
            };
            var methodInfo = typeof(UnityEngine.Transform).GetMethod(nameof(UnityEngine.Transform.LookAt), paramTypes);

            var result = new Tool_Reflection().MethodFind(
                filter: new MethodRef(methodInfo),
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                parametersMatchLevel: 2);

            ResultValidation(result);
            yield return null;
        }
        [UnityTest]
        public IEnumerator MethodFind_UnityEditor_Build_NamedBuildTarget_TargetName()
        {
            var classType = typeof(UnityEditor.Build.NamedBuildTarget);
            var name = nameof(UnityEditor.Build.NamedBuildTarget.TargetName);
            var methodInfo = classType.GetProperty(name);

            var result = new Tool_Reflection().MethodFind(
                filter: new MethodRef(methodInfo),
                knownNamespace: true);

            ResultValidation(result);
            yield return null;
        }
    }
}
