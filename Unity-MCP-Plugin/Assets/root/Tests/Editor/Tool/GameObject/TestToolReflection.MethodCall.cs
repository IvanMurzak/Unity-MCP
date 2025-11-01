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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolReflection : BaseTest
    {
        [UnityTest]
        public IEnumerator MethodCall_UnityEditor_EditorUserBuildSettings_get_activeBuildTarget()
        {
            var classType = typeof(UnityEditor.EditorUserBuildSettings);
            var name = "get_" + nameof(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
            var methodInfo = classType.GetMethod(name);

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: new MethodRef(methodInfo)));

            yield return null;
        }
        [UnityTest]
        public IEnumerator MethodCall_UnityEditor_Build_NamedBuildTarget_get_TargetName()
        {
            var classType = typeof(UnityEditor.Build.NamedBuildTarget);
            var name = "get_" + nameof(UnityEditor.Build.NamedBuildTarget.TargetName);
            var methodInfo = classType.GetMethod(name);

            var obj = new UnityEditor.Build.NamedBuildTarget();
            var serializedObj = McpPlugin.Instance!.McpRunner.Reflector.Serialize(obj);

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: new MethodRef(methodInfo),
                targetObject: serializedObj));

            yield return null;
        }
        [UnityTest]
        public IEnumerator MethodCall_UnityEngine_Application_get_platform()
        {
            var classType = typeof(UnityEngine.Application);
            var name = "get_" + nameof(UnityEngine.Application.platform);
            var methodInfo = classType.GetMethod(name);
            var MethodRef = new MethodRef(methodInfo);

            UnityEngine.Debug.Log($"Input: {MethodRef}\n");

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: MethodRef));

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: MethodRef,
                executeInMainThread: true));

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: MethodRef,
                executeInMainThread: true,
                methodNameMatchLevel: 6));

            yield return null;
        }
        [UnityTest]
        public IEnumerator MethodCall_UnityEngine_Transform_LookAt()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            var classType = typeof(UnityEngine.Transform);
            var name = nameof(UnityEngine.Transform.LookAt);
            var methodInfo = classType.GetMethod(name, new[] { typeof(UnityEngine.Transform) });
            var MethodRef = new MethodRef(methodInfo);

            UnityEngine.Debug.Log($"Input: {MethodRef}\n");

            var go1 = new UnityEngine.GameObject("1");
            var go2 = new UnityEngine.GameObject("2");

            go1.transform.position = new UnityEngine.Vector3(0, 0, 0);
            go2.transform.position = new UnityEngine.Vector3(1, 0, 0);

            var serializedTransform1 = reflector.Serialize(go1.transform, logger: McpPlugin.Instance.Logger);
            var serializedTransform2 = reflector.Serialize(go2.transform, logger: McpPlugin.Instance.Logger, name: "target");

            UnityEngine.Debug.Log($"Serialized transforms");
            UnityEngine.Debug.Log($"Transform 1: {serializedTransform1}");
            UnityEngine.Debug.Log($"Transform 2: {serializedTransform2}");

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: MethodRef,
                targetObject: serializedTransform1,
                inputParameters: new SerializedMemberList(serializedTransform2),
                executeInMainThread: true));

            UnityEngine.Debug.Log($"Method Call #1 completed, checking results...");

            Assert.LessOrEqual(
                UnityEngine.Vector3.Distance(UnityEngine.Vector3.right, go1.transform.forward),
                0.0001f,
                $"Transform.forward should be {UnityEngine.Vector3.right}.");
            go1.transform.rotation = UnityEngine.Quaternion.identity;

            ResultValidation(new Tool_Reflection().MethodCall(
                filter: MethodRef,
                targetObject: serializedTransform1,
                inputParameters: new SerializedMemberList(serializedTransform2),
                executeInMainThread: true,
                methodNameMatchLevel: 6));

            UnityEngine.Debug.Log($"Method Call #2 completed, checking results...");

            Assert.LessOrEqual(
                UnityEngine.Vector3.Distance(UnityEngine.Vector3.right, go1.transform.forward),
                0.0001f,
                $"Transform.forward should be {UnityEngine.Vector3.right}.");

            yield return null;
        }
    }
}
