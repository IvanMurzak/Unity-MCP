/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Serializable]
    [Description("Reference to UnityEngine.Object asset instance. " +
        "It could be Material, ScriptableObject, Prefab, and any other Asset. " +
        "Anything located in the Assets and Packages folders.")]
    public class AssetObjectRef : ObjectRef
    {
        public static partial class AssetObjectRefProperty
        {
            public const string AssetType = "assetType";
            public const string AssetPath = "assetPath";
            public const string AssetGuid = "assetGuid";

            public static IEnumerable<string> All => ObjectRefProperty.All.Concat(new[]
            {
                AssetType,
                AssetPath,
                AssetGuid
            });
        }
        [JsonInclude, JsonPropertyName(ObjectRefProperty.InstanceID)]
        [Description("instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'.")]
        public override int InstanceID { get; set; } = 0;

        [JsonInclude, JsonPropertyName(AssetObjectRefProperty.AssetType)]
        [Description("Type of the asset.")]
        public Type? AssetType { get; set; }

        [JsonInclude, JsonPropertyName(AssetObjectRefProperty.AssetPath)]
        [Description("Path to the asset within the project. Starts with 'Assets/'")]
        public string? AssetPath { get; set; }

        [JsonInclude, JsonPropertyName(AssetObjectRefProperty.AssetGuid)]
        [Description("Unique identifier for the asset.")]
        public string? AssetGuid { get; set; }

        public AssetObjectRef() : this(id: 0) { }
        public AssetObjectRef(int id) => InstanceID = id;
#if UNITY_EDITOR
        public AssetObjectRef(string assetPath) : this(
            UnityEditor.AssetDatabase.LoadAssetAtPath(
                assetPath,
                UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(
                    assetPath)
                ?? throw new ArgumentException($"Cannot determine asset type at path '{assetPath}'."))
            ?? throw new ArgumentException($"No asset found at path '{assetPath}'."))
        {
            // empty
        }
#else
        public AssetObjectRef(string assetPath) : this()
        {
            AssetPath = assetPath;
        }
#endif
        public AssetObjectRef(UnityEngine.Object? obj, bool throwIfNotAnAsset = true) : base(obj)
        {
#if UNITY_EDITOR
            if (obj != null)
            {
                if (!obj.IsAsset())
                {
                    if (throwIfNotAnAsset)
                        throw new ArgumentException($"Provided object (InstanceID={obj.GetInstanceID()}) is not an asset.");
                    return;
                }
                AssetType = obj.GetType();
                AssetPath = obj.GetAssetPath();
                AssetGuid = !StringUtils.IsNullOrEmpty(AssetPath)
                    ? UnityEditor.AssetDatabase.AssetPathToGUID(AssetPath)
                    : null;
            }
#endif
        }

        [JsonIgnore]
        public virtual bool IsValid
        {
            get
            {
                if (InstanceID != 0)
                    return true;

                if (!StringUtils.IsNullOrEmpty(AssetPath)
                    && (AssetPath!.StartsWith("Assets/") || AssetPath.StartsWith("Packages/")))
                    return true;

                if (!StringUtils.IsNullOrEmpty(AssetGuid))
                    return true;

                return false;
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (InstanceID != 0)
                stringBuilder.Append($"{ObjectRefProperty.InstanceID}={InstanceID}");

            if (!StringUtils.IsNullOrEmpty(AssetPath))
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append($"{AssetObjectRefProperty.AssetPath}={AssetPath}");
            }

            if (!StringUtils.IsNullOrEmpty(AssetGuid))
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append($"{AssetObjectRefProperty.AssetGuid}={AssetGuid}");
            }
            if (stringBuilder.Length == 0)
                return $"{ObjectRefProperty.InstanceID}={InstanceID}";

            return stringBuilder.ToString();
        }
    }
}
