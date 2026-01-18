using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class SerializeTest : BaseTest
    {
        [UnityTest]
        public IEnumerator Serialize_GameObject_WithComponent_ListOfNullColliders()
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_ColliderList");
            var component = go.AddComponent<ColliderListTestScript>();

            // Set the list to contain two null objects
            component.colliderList.Add(null!);
            component.colliderList.Add(null!);

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(component);

            // Wait 3 frames to ensure everything is initialized
            yield return null;
            yield return null;
            yield return null;

            var serializedComponent = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var jsonComponent = serializedComponent.ToJson(reflector);
            Debug.Log($"[{nameof(SerializeTest)}] Serialized ColliderListTestScript:\n{jsonComponent}");

            // Serialize the GameObject with recursive set to true
            var serialized = reflector.Serialize(
                go,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector);
            Debug.Log($"[{nameof(SerializeTest)}] Serialized GameObject with null collider list:\n{json}");

            // Validate that the serialization completed without errors
            Assert.IsNotNull(serializedComponent, "Serialized component result should not be null.");
            Assert.IsNotNull(serializedComponent.fields, "Serialized component fields should not be null.");

            // Validate that the colliderList field exists in the component
            var colliderListField = serializedComponent.fields.FirstOrDefault(f => f.name == "colliderList");
            Assert.IsNotNull(colliderListField, "colliderList field should be serialized.");

            var deserializedColliderList = reflector.Deserialize(
                colliderListField!,
                typeof(List<UnityEngine.Collider>),
                logger: _logger) as List<UnityEngine.Collider>;

            Assert.IsNotNull(deserializedColliderList, "Deserialized collider list should not be null.");

            var str = string.Join(", ", deserializedColliderList!.Select(c => c == null ? "null" : c.name));
            Debug.Log($"[{nameof(SerializeTest)}] deserializedColliderList: {str}");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_GameObject_WithComponent_ListOfDestroyedColliders()
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_ColliderList");
            var component = go.AddComponent<ColliderListTestScript>();

            // Create two GameObjects with colliders
            var goCapsule = new GameObject("TestGameObject_CapsuleCollider");
            var capsuleCollider = goCapsule.AddComponent<CapsuleCollider>();

            var goBox = new GameObject("TestGameObject_BoxCollider");
            var boxCollider = goBox.AddComponent<BoxCollider>();

            // Add the colliders to the list
            component.colliderList.Add(capsuleCollider);
            component.colliderList.Add(boxCollider);

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(component);

            // Destroy the collider components
            UnityEngine.Object.DestroyImmediate(capsuleCollider);
            UnityEngine.Object.DestroyImmediate(boxCollider);

            // Wait 3 frames to ensure everything is processed
            yield return null;
            yield return null;
            yield return null;

            var serializedComponent = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var jsonComponent = serializedComponent.ToJson(reflector);
            Debug.Log($"[{nameof(SerializeTest)}] Serialized ColliderListTestScript with destroyed colliders:\n{jsonComponent}");

            // Serialize the GameObject with recursive set to true
            var serialized = reflector.Serialize(
                go,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector);
            Debug.Log($"[{nameof(SerializeTest)}] Serialized GameObject with destroyed collider list:\n{json}");

            // Validate that the serialization completed without errors
            Assert.IsNotNull(serializedComponent, "Serialized component result should not be null.");
            Assert.IsNotNull(serializedComponent.fields, "Serialized component fields should not be null.");

            // Validate that the colliderList field exists in the component
            var colliderListField = serializedComponent.fields.FirstOrDefault(f => f.name == "colliderList");
            Assert.IsNotNull(colliderListField, "colliderList field should be serialized.");

            var deserializedColliderList = reflector.Deserialize(
                colliderListField!,
                typeof(List<UnityEngine.Collider>),
                logger: _logger) as List<UnityEngine.Collider>;

            Assert.IsNotNull(deserializedColliderList, "Deserialized collider list should not be null.");

            var str = string.Join(", ", deserializedColliderList!.Select(c => c == null ? "null" : c.name));
            Debug.Log($"[{nameof(SerializeTest)}] deserializedColliderList: {str}");

            // Cleanup the additional GameObjects
            UnityEngine.Object.DestroyImmediate(goCapsule);
            UnityEngine.Object.DestroyImmediate(goBox);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_BoxCollider_ShouldNotContainMaterialField()
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            // Create a GameObject and add BoxCollider component
            var go = new GameObject("TestGameObject_BoxCollider");
            var boxCollider = go.AddComponent<BoxCollider>();

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(boxCollider);

            // Wait for initialization
            yield return null;
            yield return null;
            yield return null;

            // Serialize the BoxCollider component
            var serialized = reflector.Serialize(
                boxCollider,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector);
            Debug.Log($"[{nameof(SerializeTest)}] Serialized BoxCollider:\n{json}");

            // Validate that the serialization completed without errors
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Check that no field with name "material" exists (should be ignored by custom reflection converter)
            var materialField = FindMemberByName(serialized.fields, "material");
            Assert.IsNull(materialField, "Field 'material' should not exist in serialized BoxCollider (should be ignored by custom reflection converter).");

            // Check that no property with name "material" exists (should be ignored by custom reflection converter)
            var materialProperty = FindMemberByName(serialized.props, "material");
            Assert.IsNull(materialProperty, "Property 'material' should not exist in serialized BoxCollider (should be ignored by custom reflection converter).");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);

            yield return null;
        }

        private SerializedMember FindMemberByName(IEnumerable<SerializedMember> members, string name)
        {
            if (members == null)
                return null;

            foreach (var member in members)
            {
                if (member.name == name)
                    return member;

                // Recursively check nested fields
                var nestedInFields = FindMemberByName(member.fields, name);
                if (nestedInFields != null)
                    return nestedInFields;

                // Recursively check nested props
                var nestedInProps = FindMemberByName(member.props, name);
                if (nestedInProps != null)
                    return nestedInProps;
            }

            return null;
        }
    }
}