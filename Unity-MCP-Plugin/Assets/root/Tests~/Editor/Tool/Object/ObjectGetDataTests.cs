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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ObjectGetDataTests : BaseTest
    {
        [Test]
        public void GetData_ValidGameObject_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("TestObject");
            var objectRef = new ObjectRef(go);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("TestObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.GameObject", result.typeName, "TypeName should be GameObject");
        }

        [Test]
        public void GetData_ValidComponent_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("TestObjectWithComponent");
            var transform = go.transform;
            var objectRef = new ObjectRef(transform);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("TestObjectWithComponent", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.Transform", result.typeName, "TypeName should be Transform");
        }

        [Test]
        public void GetData_MeshRenderer_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("TestObjectWithMeshRenderer");
            var meshRenderer = go.AddComponent<MeshRenderer>();
            var objectRef = new ObjectRef(meshRenderer);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("TestObjectWithMeshRenderer", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.MeshRenderer", result.typeName, "TypeName should be MeshRenderer");
        }

        [Test]
        public void GetData_InvalidObjectRef_ZeroInstanceID_ThrowsArgumentException()
        {
            // Arrange
            var tool = new Tool_Object();
            var objectRef = new ObjectRef(0);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => tool.GetData(objectRef));

            Assert.IsNotNull(ex);
            Assert.AreEqual("objectRef", ex!.ParamName, "Exception should specify objectRef parameter");
        }

        [Test]
        public void GetData_NonExistentInstanceID_ThrowsException()
        {
            // Arrange
            var tool = new Tool_Object();
            // Use a very large instance ID that doesn't exist
            var objectRef = new ObjectRef(999999999);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => tool.GetData(objectRef));

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("Not found"), "Exception message should indicate object was not found");
        }

        [Test]
        public void GetData_DestroyedGameObject_ThrowsException()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("ObjectToDestroy");
            var objectRef = new ObjectRef(go);

            // Destroy the object
            UnityEngine.Object.DestroyImmediate(go);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => tool.GetData(objectRef));

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("Not found"), "Exception message should indicate object was not found");
        }

        [Test]
        public void GetData_GameObjectWithPosition_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("PositionedObject");
            go.transform.position = new Vector3(1f, 2f, 3f);
            var objectRef = new ObjectRef(go);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("PositionedObject", result!.name);
            Assert.AreEqual("UnityEngine.GameObject", result.typeName);
        }

        [Test]
        public void GetData_Camera_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("CameraObject");
            var camera = go.AddComponent<Camera>();
            var objectRef = new ObjectRef(camera);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("CameraObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.Camera", result.typeName, "TypeName should be Camera");
        }

        [Test]
        public void GetData_Light_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("LightObject");
            var light = go.AddComponent<Light>();
            var objectRef = new ObjectRef(light);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("LightObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.Light", result.typeName, "TypeName should be Light");
        }

        [Test]
        public void GetData_BoxCollider_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("ColliderObject");
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 3f, 4f);
            var objectRef = new ObjectRef(collider);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("ColliderObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.BoxCollider", result.typeName, "TypeName should be BoxCollider");
        }

        [Test]
        public void GetData_Rigidbody_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("RigidbodyObject");
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 5f;
            var objectRef = new ObjectRef(rb);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("RigidbodyObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.Rigidbody", result.typeName, "TypeName should be Rigidbody");
        }

        [Test]
        public void GetData_AudioSource_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("AudioObject");
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.volume = 0.5f;
            var objectRef = new ObjectRef(audioSource);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("AudioObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.AudioSource", result.typeName, "TypeName should be AudioSource");
        }

        [Test]
        public void GetData_InactiveGameObject_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var go = new GameObject("InactiveObject");
            go.SetActive(false);
            var objectRef = new ObjectRef(go);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("InactiveObject", result!.name, "Name should match GameObject name");
            Assert.AreEqual("UnityEngine.GameObject", result.typeName, "TypeName should be GameObject");
        }

        [Test]
        public void GetData_ChildGameObject_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var parent = new GameObject("Parent");
            var child = new GameObject("ChildObject");
            child.transform.SetParent(parent.transform);
            var objectRef = new ObjectRef(child);

            // Act
            var result = tool.GetData(objectRef);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("ChildObject", result!.name, "Name should match child GameObject name");
            Assert.AreEqual("UnityEngine.GameObject", result.typeName, "TypeName should be GameObject");
        }

        [Test]
        public void GetData_Material_ReturnsSerializedData()
        {
            // Arrange
            var tool = new Tool_Object();
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";
            var objectRef = new ObjectRef(material);

            try
            {
                // Act
                var result = tool.GetData(objectRef);

                // Assert
                Assert.IsNotNull(result, "Result should not be null");
                Assert.AreEqual("TestMaterial", result!.name, "Name should match material name");
                Assert.AreEqual("UnityEngine.Material", result.typeName, "TypeName should be Material");
            }
            finally
            {
                // Cleanup
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
