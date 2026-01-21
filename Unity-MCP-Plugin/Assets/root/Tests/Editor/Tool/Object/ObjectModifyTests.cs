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
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ObjectModifyTests : BaseTest
    {
        [UnityTest]
        public IEnumerator Modify_RenameGameObject_Success()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("OriginalName");
            var objectRef = new ObjectRef(go);

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            var diff = new SerializedMember
            {
                props = new SerializedMemberList()
            };
            diff.props.Add(SerializedMember.FromValue(
                reflector: reflector,
                name: "name",
                type: typeof(string),
                value: "NewName"
            ));

            // Act
            var result = tool.Modify(objectRef, diff);

            // Assert
            Assert.IsTrue(result.Success, "Modify should return success");
            Assert.AreEqual("NewName", go.name, "GameObject name should be updated");
            Assert.AreEqual("NewName", result.Data!.name, "Result data should contain updated name");

            // Cleanup
            GameObject.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Modify_TransformPosition_Success()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("TestTransform");
            var objectRef = new ObjectRef(go.transform);

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            var posMember = new SerializedMember
            {
                name = "position",
                fields = new SerializedMemberList()
            };
            // Assuming we can add fields for Vector3 members
            posMember.fields.Add(SerializedMember.FromValue(
                reflector: reflector,
                name: "x",
                type: typeof(float),
                value: 10.0f
            ));
            posMember.fields.Add(SerializedMember.FromValue(
                reflector: reflector,
                name: "y",
                type: typeof(float),
                value: 20.0f
            ));
            posMember.fields.Add(SerializedMember.FromValue(
                reflector: reflector,
                name: "z",
                type: typeof(float),
                value: 30.0f
            ));

            var diff = new SerializedMember
            {
                props = new SerializedMemberList()
            };
            diff.props.Add(posMember);

            // Act
            var result = tool.Modify(objectRef, diff);

            // Assert
            Assert.IsTrue(result.Success, "Modify should return success");
            var pos = go.transform.position;
            Assert.AreEqual(10f, pos.x, 0.001f);
            Assert.AreEqual(20f, pos.y, 0.001f);
            Assert.AreEqual(30f, pos.z, 0.001f);

            // Cleanup
            GameObject.DestroyImmediate(go);
            yield return null;
        }

        [Test]
        public void Modify_InvalidObjectRef_ThrowsException()
        {
            // Arrange
            var tool = new Tool_Object();
            var objectRef = new ObjectRef(new GameObject("Temp"));
            GameObject.DestroyImmediate(objectRef.FindObject()); // Make it invalid

            var diff = new SerializedMember();

            // Act & Assert
            Assert.Throws<Exception>(() => tool.Modify(objectRef, diff));
        }

        [Test]
        public void Modify_NullDiff_ThrowsArgumentNullException()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("TestObject");
            var objectRef = new ObjectRef(go);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tool.Modify(objectRef, null!));

            // Cleanup
            GameObject.DestroyImmediate(go);
        }
    }
}
