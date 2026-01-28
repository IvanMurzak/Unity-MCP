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
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests for verifying that struct fields and properties can be partially populated
    /// without losing existing values for unspecified members.
    /// </summary>
    public class StructPopulationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        [UnityTest]
        public IEnumerator Populate_Vector3Field_PartialUpdate_PreservesUnspecifiedValues()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            var initialVector = new Vector3(1f, 2f, 3f);
            comp.vector3Field = initialVector;

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a partial update that only modifies the 'x' field
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            var vector3Diff = new SerializedMember { name = "vector3Field" };
            vector3Diff.fields = new SerializedMemberList();
            vector3Diff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "x", type: typeof(float), value: 10f));
            componentDiff.AddField(vector3Diff);

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");
            Assert.AreEqual(10f, comp.vector3Field.x, 0.001f, "X should be updated to 10");
            Assert.AreEqual(initialVector.y, comp.vector3Field.y, 0.001f, "Y should be preserved");
            Assert.AreEqual(initialVector.z, comp.vector3Field.z, 0.001f, "Z should be preserved");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Populate_ColorField_PartialUpdate_PreservesUnspecifiedValues()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            var initialColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            comp.colorField = initialColor;

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a partial update that only modifies 'r' and 'g'
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            var colorDiff = new SerializedMember { name = "colorField" };
            colorDiff.fields = new SerializedMemberList();
            colorDiff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "r", type: typeof(float), value: 1.0f));
            colorDiff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "g", type: typeof(float), value: 0.5f));
            componentDiff.AddField(colorDiff);

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");
            Assert.AreEqual(1.0f, comp.colorField.r, 0.001f, "R should be updated to 1.0");
            Assert.AreEqual(0.5f, comp.colorField.g, 0.001f, "G should be updated to 0.5");
            Assert.AreEqual(initialColor.b, comp.colorField.b, 0.001f, "B should be preserved");
            Assert.AreEqual(initialColor.a, comp.colorField.a, 0.001f, "A should be preserved");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Populate_CustomStruct_PartialUpdate_PreservesUnspecifiedValues()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            var initialStruct = new TestStruct
            {
                floatValue = 1.5f,
                intValue = 42,
                stringValue = "original",
                nestedVector = new Vector3(1f, 2f, 3f)
            };
            comp.customStructField = initialStruct;

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a partial update that only modifies 'intValue'
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            var structDiff = new SerializedMember { name = "customStructField" };
            structDiff.fields = new SerializedMemberList();
            structDiff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "intValue", type: typeof(int), value: 100));
            componentDiff.AddField(structDiff);

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");
            Assert.AreEqual(100, comp.customStructField.intValue, "intValue should be updated to 100");
            Assert.AreEqual(initialStruct.floatValue, comp.customStructField.floatValue, 0.001f, "floatValue should be preserved");
            Assert.AreEqual(initialStruct.stringValue, comp.customStructField.stringValue, "stringValue should be preserved");
            Assert.AreEqual(initialStruct.nestedVector, comp.customStructField.nestedVector, "nestedVector should be preserved");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Populate_NestedStruct_PartialUpdate_PreservesUnspecifiedValues()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            var initialStruct = new TestStruct
            {
                floatValue = 1.5f,
                intValue = 42,
                stringValue = "original",
                nestedVector = new Vector3(10f, 20f, 30f)
            };
            comp.customStructField = initialStruct;

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a partial update that only modifies 'nestedVector.x'
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            var vectorDiff = new SerializedMember { name = "nestedVector" };
            vectorDiff.fields = new SerializedMemberList();
            vectorDiff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "x", type: typeof(float), value: 99f));

            var structDiff = new SerializedMember { name = "customStructField" };
            structDiff.fields = new SerializedMemberList();
            structDiff.fields.Add(vectorDiff);
            componentDiff.AddField(structDiff);

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");
            Assert.AreEqual(99f, comp.customStructField.nestedVector.x, 0.001f, "nestedVector.x should be updated to 99");
            Assert.AreEqual(initialStruct.nestedVector.y, comp.customStructField.nestedVector.y, 0.001f, "nestedVector.y should be preserved");
            Assert.AreEqual(initialStruct.nestedVector.z, comp.customStructField.nestedVector.z, 0.001f, "nestedVector.z should be preserved");
            Assert.AreEqual(initialStruct.intValue, comp.customStructField.intValue, "intValue should be preserved");
            Assert.AreEqual(initialStruct.floatValue, comp.customStructField.floatValue, 0.001f, "floatValue should be preserved");
            Assert.AreEqual(initialStruct.stringValue, comp.customStructField.stringValue, "stringValue should be preserved");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Populate_MultipleStructFields_PartialUpdate_PreservesUnspecifiedValues()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            comp.vector3Field = new Vector3(1f, 2f, 3f);
            comp.colorField = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            comp.vector2Field = new Vector2(5f, 6f);

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create partial updates for multiple struct fields
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            // Update only vector3Field.y
            var vector3Diff = new SerializedMember { name = "vector3Field" };
            vector3Diff.fields = new SerializedMemberList();
            vector3Diff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "y", type: typeof(float), value: 100f));
            componentDiff.AddField(vector3Diff);

            // Update only colorField.a
            var colorDiff = new SerializedMember { name = "colorField" };
            colorDiff.fields = new SerializedMemberList();
            colorDiff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "a", type: typeof(float), value: 1.0f));
            componentDiff.AddField(colorDiff);

            // Update only vector2Field.x
            var vector2Diff = new SerializedMember { name = "vector2Field" };
            vector2Diff.fields = new SerializedMemberList();
            vector2Diff.fields.Add(SerializedMember.FromValue(reflector: reflector, name: "x", type: typeof(float), value: 50f));
            componentDiff.AddField(vector2Diff);

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");

            // Verify vector3Field
            Assert.AreEqual(1f, comp.vector3Field.x, 0.001f, "vector3Field.x should be preserved");
            Assert.AreEqual(100f, comp.vector3Field.y, 0.001f, "vector3Field.y should be updated to 100");
            Assert.AreEqual(3f, comp.vector3Field.z, 0.001f, "vector3Field.z should be preserved");

            // Verify colorField
            Assert.AreEqual(0.1f, comp.colorField.r, 0.001f, "colorField.r should be preserved");
            Assert.AreEqual(0.2f, comp.colorField.g, 0.001f, "colorField.g should be preserved");
            Assert.AreEqual(0.3f, comp.colorField.b, 0.001f, "colorField.b should be preserved");
            Assert.AreEqual(1.0f, comp.colorField.a, 0.001f, "colorField.a should be updated to 1.0");

            // Verify vector2Field
            Assert.AreEqual(50f, comp.vector2Field.x, 0.001f, "vector2Field.x should be updated to 50");
            Assert.AreEqual(6f, comp.vector2Field.y, 0.001f, "vector2Field.y should be preserved");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Populate_StructField_FullReplacement_Works()
        {
            // Arrange
            var go = new GameObject("TestStructPopulation");
            var comp = go.AddComponent<StructFieldPopulationTestScript>();

            // Set initial values
            comp.vector3Field = new Vector3(1f, 2f, 3f);

            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a full replacement (using valueJsonElement, not nested fields)
            var componentDiff = SerializedMember.FromValue(
                reflector: reflector,
                name: nameof(StructFieldPopulationTestScript),
                type: typeof(StructFieldPopulationTestScript),
                value: null
            );

            // Full replacement - all fields are specified
            var newVector = new Vector3(100f, 200f, 300f);
            componentDiff.AddField(SerializedMember.FromValue(reflector: reflector, name: "vector3Field", type: typeof(Vector3), value: newVector));

            // Act
            var objToModify = (object)comp;
            var logs = new Logs();
            var success = reflector.TryPopulate(
                ref objToModify,
                data: componentDiff,
                logs: logs,
                logger: _logger);

            // Assert
            Assert.IsTrue(success, $"TryPopulate should succeed. Logs: {logs}");
            Assert.AreEqual(newVector.x, comp.vector3Field.x, 0.001f, "vector3Field.x should be updated");
            Assert.AreEqual(newVector.y, comp.vector3Field.y, 0.001f, "vector3Field.y should be updated");
            Assert.AreEqual(newVector.z, comp.vector3Field.z, 0.001f, "vector3Field.z should be updated");

            yield return null;
        }
    }
}
