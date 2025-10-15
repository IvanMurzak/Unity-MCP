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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Common.Model.Unity
{
    [Description(@"Find GameObject in opened Prefab or in the active Scene.")]
    public class GameObjectRef : AssetObjectRef
    {
        public static partial class GameObjectRefProperty
        {
            public const string Path = "path";
            public const string Name = "name";

            public static IEnumerable<string> All => AssetObjectRefProperty.All.Concat(new[]
            {
                Path,
                Name
            });
        }
        [JsonInclude, JsonPropertyName(ObjectRefProperty.InstanceID)]
        [Description(
            "instanceID of the UnityEngine.Object. If it is '0' and '"
            + GameObjectRefProperty.Path + "', '"
            + GameObjectRefProperty.Name + "', '"
            + AssetObjectRefProperty.AssetPath + "' and '"
            + AssetObjectRefProperty.AssetGuid
            + "' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)")]
        public override int InstanceID { get; set; } = 0;

        [JsonInclude, JsonPropertyName(GameObjectRefProperty.Path)]
        [Description("Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2.")]
        public string? Path { get; set; } = null;

        [JsonInclude, JsonPropertyName(GameObjectRefProperty.Name)]
        [Description("Name of a GameObject in hierarchy. Priority: 3.")]
        public string? Name { get; set; } = null;

        [JsonIgnore]
        public override bool IsValid
        {
            get
            {
                if (!string.IsNullOrEmpty(Path))
                    return true;
                if (!string.IsNullOrEmpty(Name))
                    return true;

                return base.IsValid;
            }
        }

        public GameObjectRef() { }
        public GameObjectRef(int instanceID)
        {
            this.InstanceID = instanceID;
        }

        public override string ToString()
        {
            if (InstanceID != 0)
                return $"GameObject {ObjectRefProperty.InstanceID}='{InstanceID}'";
            if (!string.IsNullOrEmpty(Path))
                return $"GameObject {GameObjectRefProperty.Path}='{Path}'";
            if (!string.IsNullOrEmpty(Name))
                return $"GameObject {GameObjectRefProperty.Name}='{Name}'";
            if (!string.IsNullOrEmpty(AssetPath))
                return $"GameObject {AssetObjectRefProperty.AssetPath}='{AssetPath}'";
            if (!string.IsNullOrEmpty(AssetGuid))
                return $"GameObject {AssetObjectRefProperty.AssetGuid}='{AssetGuid}'";
            return "GameObject unknown";
        }
    }
}
