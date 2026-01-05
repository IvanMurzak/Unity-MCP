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
        static Reflector CreateDefaultReflector()
        {
            var reflector = new Reflector();

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
            reflector.Converters.BlacklistType(typeof(UnityEngine.LowLevelPhysics.GeometryHolder));
#endif
            // Redundant text data
            reflector.Converters.BlacklistType(typeof(UnityEngine.Font));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.TextElement));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.FontFeatureTable));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.SpriteGlyph));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.TextFontWeight));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.Character));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.SpriteCharacter));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.SpriteGlyph));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Text.TextShaderUtilities));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.Glyph));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.GlyphRect));
            reflector.Converters.BlacklistType(typeof(UnityEngine.TextCore.GlyphMetrics));

            // TextMeshPro types
            var tmpTextElementType = TypeUtils.GetType("TMPro.TMP_TextElement");
            if (tmpTextElementType != null)
                reflector.Converters.BlacklistType(tmpTextElementType); // Heavy text data

            var tmpFontFeatureTableType = TypeUtils.GetType("TMPro.TMP_FontFeatureTable");
            if (tmpFontFeatureTableType != null)
                reflector.Converters.BlacklistType(tmpFontFeatureTableType); // Heavy font data

            var tmpFontWeightPairType = TypeUtils.GetType("TMPro.TMP_FontWeightPair");
            if (tmpFontWeightPairType != null)
                reflector.Converters.BlacklistType(tmpFontWeightPairType); // Heavy font data

            var tmpFontAssetCreationSettingsType = TypeUtils.GetType("TMPro.FontAssetCreationSettings");
            if (tmpFontAssetCreationSettingsType != null)
                reflector.Converters.BlacklistType(tmpFontAssetCreationSettingsType); // Heavy font data

            var tmpFaceInfo_LegacyType = TypeUtils.GetType("TMPro.FaceInfo_Legacy");
            if (tmpFaceInfo_LegacyType != null)
                reflector.Converters.BlacklistType(tmpFaceInfo_LegacyType); // Heavy font data

            var tmpTMP_CharacterType = TypeUtils.GetType("TMPro.TMP_Character");
            if (tmpTMP_CharacterType != null)
                reflector.Converters.BlacklistType(tmpTMP_CharacterType); // Heavy text data

            // var tmpFontAssetType = TypeUtils.GetType("TMPro.TMP_FontAsset");
            // if (tmpFontAssetType != null)
            //     reflector.Converters.BlacklistType(tmpFontAssetType); // Circular references in fallback tables

            var tmpTextInfoType = TypeUtils.GetType("TMPro.TMP_TextInfo");
            if (tmpTextInfoType != null)
                reflector.Converters.BlacklistType(tmpTextInfoType); // Heavy text data

            var tmpTMP_SpriteAnimatorType = TypeUtils.GetType("TMPro.TMP_SpriteAnimator");
            if (tmpTMP_SpriteAnimatorType != null)
                reflector.Converters.BlacklistType(tmpTMP_SpriteAnimatorType); // Heavy text data

            var tmpSpriteAssetType = TypeUtils.GetType("TMPro.TMP_SpriteAsset");
            if (tmpSpriteAssetType != null)
                reflector.Converters.BlacklistType(tmpSpriteAssetType); // Circular references in fallback tables

            var tmpSpriteCharacterType = TypeUtils.GetType("TMPro.TMP_SpriteCharacter");
            if (tmpSpriteCharacterType != null)
                reflector.Converters.BlacklistType(tmpSpriteCharacterType); // Heavy sprite data

            var tmpSpriteGlyphType = TypeUtils.GetType("TMPro.TMP_SpriteGlyph");
            if (tmpSpriteGlyphType != null)
                reflector.Converters.BlacklistType(tmpSpriteGlyphType); // Heavy sprite data

            var tmpTMP_SpriteType = TypeUtils.GetType("TMPro.TMP_Sprite");
            if (tmpTMP_SpriteType != null)
                reflector.Converters.BlacklistType(tmpTMP_SpriteType); // Heavy sprite data

            var tmpTMP_GlyphType = TypeUtils.GetType("TMPro.TMP_Glyph");
            if (tmpTMP_GlyphType != null)
                reflector.Converters.BlacklistType(tmpTMP_GlyphType); // Heavy font data

            var tmpKerningTableType = TypeUtils.GetType("TMPro.KerningTable");
            if (tmpKerningTableType != null)
                reflector.Converters.BlacklistType(tmpKerningTableType); // Heavy font data

            var tmpKerningPairType = TypeUtils.GetType("TMPro.KerningPair");
            if (tmpKerningPairType != null)
                reflector.Converters.BlacklistType(tmpKerningPairType); // Heavy font data

            // var tmpColorGradientType = TypeUtils.GetType("TMPro.TMP_ColorGradient");
            // if (tmpColorGradientType != null)
            //     reflector.Converters.BlacklistType(tmpColorGradientType); // Heavy asset data

            // var tmpStyleSheetType = TypeUtils.GetType("TMPro.TMP_StyleSheet");
            // if (tmpStyleSheetType != null)
            //     reflector.Converters.BlacklistType(tmpStyleSheetType); // Heavy asset data

            // var tmpStyleType = TypeUtils.GetType("TMPro.TMP_Style");
            // if (tmpStyleType != null)
            //     reflector.Converters.BlacklistType(tmpStyleType); // Heavy asset data

            // var tmpSettingsType = TypeUtils.GetType("TMPro.TMP_Settings");
            // if (tmpSettingsType != null)
            //     reflector.Converters.BlacklistType(tmpSettingsType); // Contains global fallback lists

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
