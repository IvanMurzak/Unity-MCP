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
using com.IvanMurzak.Unity.MCP.JsonConverters;
using com.IvanMurzak.Unity.MCP.Reflection.Converter;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        public Reflector CreateDefaultReflector()
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
            var componentConverter = new UnityEngine_Component_ReflectionConverter();
            reflector.Converters.Add(componentConverter);
            reflector.Converters.Add(new UnityEngine_Transform_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Renderer_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_MeshFilter_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Collider_ReflectionConverter(componentConverter));

            // Assets
            reflector.Converters.Add(new UnityEngine_Material_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Texture_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Sprite_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_TextAsset_ReflectionConverter());

            // Blacklist types
            // ---------------------------------------------------------

            // System types
            reflector.Converters.BlacklistType(typeof(System.Delegate));
            reflector.Converters.BlacklistType(typeof(System.EventHandler));
            reflector.Converters.BlacklistType(typeof(System.EventHandler<>));
            reflector.Converters.BlacklistType(typeof(System.MulticastDelegate));
            reflector.Converters.BlacklistType(typeof(System.IntPtr));
            reflector.Converters.BlacklistType(typeof(System.UIntPtr));
            reflector.Converters.BlacklistType(typeof(System.Reflection.MemberInfo));
            reflector.Converters.BlacklistType(typeof(System.Threading.CancellationToken));
            reflector.Converters.BlacklistType(typeof(System.Span<>));
            reflector.Converters.BlacklistType(typeof(System.ReadOnlySpan<>));

#if UNITY_2023_1_OR_NEWER
            reflector.Converters.BlacklistType(typeof(UnityEngine.LowLevelPhysics.GeometryHolder));
#endif
            // Redundant text data
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.FontFeatureTable));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Glyph));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.GlyphRect));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.GlyphMetrics));

            // Redundant TextMeshPro data
            reflector.Converters.BlacklistType("TMPro.TMP_TextInfo"); // Heavy text data
            reflector.Converters.BlacklistType("TMPro.TMP_TextElement"); // Heavy text data
            reflector.Converters.BlacklistType("TMPro.TMP_FontFeatureTable"); // Heavy font data
            reflector.Converters.BlacklistType("TMPro.TMP_FontWeightPair"); // Heavy font data
            reflector.Converters.BlacklistType("TMPro.FaceInfo_Legacy"); // Heavy font data

            // Redundant RenderPipeline data
            reflector.Converters.BlacklistType("UnityEngine.Rendering.RTHandle"); // Can't be utilized
            reflector.Converters.BlacklistType("UnityEngine.Experimental.Rendering.RTHandle"); // Can't be utilized

            // Photon IL-weaved types
            reflector.Converters.BlacklistType("Fusion.NetworkBehaviourBuffer");

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
    }
}
