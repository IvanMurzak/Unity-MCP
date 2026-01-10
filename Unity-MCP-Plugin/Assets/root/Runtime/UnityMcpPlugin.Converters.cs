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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.JsonConverters;
using com.IvanMurzak.Unity.MCP.Reflection.Converter;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        Reflector CreateDefaultReflector()
        {
            var reflector = new Reflector();
            reflector.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;

            // Remove converters that are not needed in Unity
            reflector.Converters.Remove<GenericReflectionConverter<object>>();
            reflector.Converters.Remove<ArrayReflectionConverter>();

            // Add Unity-specific converters
            reflector.Converters.Add(new UnityGenericReflectionConverter<object>());
            reflector.Converters.Add(new UnityArrayReflectionConverter());

            // Unity types
            reflector.Converters.Add(new UnityEngine_Color32_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Color_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Matrix4x4_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Quaternion_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Vector2_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Vector2Int_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Vector3_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Vector3Int_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Vector4_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Bounds_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_BoundsInt_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Rect_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_RectInt_ReflectionConverter());

            // Unity objects
            reflector.Converters.Add(new UnityEngine_Object_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_GameObject_ReflectionConverter());

            // Components
            reflector.Converters.Add(new UnityEngine_Component_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Transform_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Renderer_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_MeshFilter_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Collider_ReflectionConverter());

            // Assets
            reflector.Converters.Add(new UnityEngine_Material_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Texture_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Sprite_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_TextAsset_ReflectionConverter());

            // Blacklist types
            // ---------------------------------------------------------
#if UNITY_2023_1_OR_NEWER
            BlacklistType(typeof(UnityEngine.LowLevelPhysics.GeometryHolder));
#endif
            // Redundant text data
            BlacklistType(typeof(UnityEngine.TextCore.Text.FontFeatureTable));
            BlacklistType(typeof(UnityEngine.TextCore.Glyph));
            BlacklistType(typeof(UnityEngine.TextCore.GlyphRect));
            BlacklistType(typeof(UnityEngine.TextCore.GlyphMetrics));

            // Redundant TextMeshPro data
            BlacklistType("TMPro.TMP_TextElement"); // Heavy text data
            BlacklistType("TMPro.TMP_FontFeatureTable"); // Heavy font data
            BlacklistType("TMPro.TMP_FontWeightPair"); // Heavy font data
            BlacklistType("TMPro.FaceInfo_Legacy"); // Heavy font data

            // Redundant RenderPipeline data
            BlacklistType("UnityEngine.Rendering.RTHandle"); // Can't be utilized
            BlacklistType("UnityEngine.Experimental.Rendering.RTHandle"); // Can't be utilized

            // Json Converters
            // ---------------------------------------------------------

            // Unity types
            reflector.JsonSerializer.AddConverter(new Color32Converter());
            reflector.JsonSerializer.AddConverter(new ColorConverter());
            reflector.JsonSerializer.AddConverter(new Matrix4x4Converter());
            reflector.JsonSerializer.AddConverter(new QuaternionConverter());
            reflector.JsonSerializer.AddConverter(new Vector2Converter());
            reflector.JsonSerializer.AddConverter(new Vector2IntConverter());
            reflector.JsonSerializer.AddConverter(new Vector3Converter());
            reflector.JsonSerializer.AddConverter(new Vector3IntConverter());
            reflector.JsonSerializer.AddConverter(new Vector4Converter());
            reflector.JsonSerializer.AddConverter(new BoundsConverter());
            reflector.JsonSerializer.AddConverter(new BoundsIntConverter());
            reflector.JsonSerializer.AddConverter(new RectConverter());
            reflector.JsonSerializer.AddConverter(new RectIntConverter());

            // Reference types
            reflector.JsonSerializer.AddConverter(new ObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new AssetObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new GameObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new ComponentRefConverter());

            return reflector;
        }

        /// <summary>
        /// Adds a type to the blacklist, preventing it from being serialized by the reflection system.
        /// Blacklisted types are skipped during serialization to avoid heavy or redundant data.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified type name (e.g., "TMPro.TMP_TextElement").</param>
        /// <returns>The current instance for method chaining.</returns>
        public UnityMcpPlugin BlacklistType(string fullTypeName)
        {
            var type = TypeUtils.GetType(fullTypeName);
            if (type != null)
                Reflector?.Converters.BlacklistType(type);
            return this;
        }

        /// <summary>
        /// Adds a type to the blacklist, preventing it from being serialized by the reflection system.
        /// Blacklisted types are skipped during serialization to avoid heavy or redundant data.
        /// </summary>
        /// <param name="type">The type to blacklist.</param>
        /// <returns>The current instance for method chaining.</returns>
        public UnityMcpPlugin BlacklistType(System.Type type)
        {
            Reflector?.Converters.BlacklistType(type);
            return this;
        }

        /// <summary>
        /// Removes a type from the blacklist, allowing it to be serialized by the reflection system again.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified type name (e.g., "TMPro.TMP_TextElement").</param>
        /// <returns>The current instance for method chaining.</returns>
        public UnityMcpPlugin RemoveBlacklistedType(string fullTypeName)
        {
            var type = TypeUtils.GetType(fullTypeName);
            if (type != null)
                Reflector?.Converters.RemoveBlacklistedType(type);
            return this;
        }

        /// <summary>
        /// Removes a type from the blacklist, allowing it to be serialized by the reflection system again.
        /// </summary>
        /// <param name="type">The type to remove from the blacklist.</param>
        /// <returns>The current instance for method chaining.</returns>
        public UnityMcpPlugin RemoveBlacklistedType(System.Type type)
        {
            Reflector?.Converters.RemoveBlacklistedType(type);
            return this;
        }
    }
}
