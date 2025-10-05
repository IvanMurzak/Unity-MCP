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
using System.ComponentModel;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.Unity.MCP.Common.Model.Unity
{
    [System.Serializable]
    [Description(@"GameObject reference. Used to find GameObject in opened Prefab or in a Scene.")]
    public class GameObjectComponentsRef : GameObjectRef
    {
        [JsonInclude, JsonPropertyName("components")]
        [Description("GameObject 'components'.")]
        public SerializedMemberList? Components { get; set; }

        public GameObjectComponentsRef() { }

        public override string ToString()
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine($"{base.ToString()}");

            if (Components != null && Components.Count > 0)
            {
                stringBuilder.AppendLine($"Components total amount: {Components.Count}");
                for (int i = 0; i < Components.Count; i++)
                    stringBuilder.AppendLine($"Component[{i}] {Components[i]}");
            }
            else
            {
                stringBuilder.AppendLine("No Components");
            }
            return stringBuilder.ToString();
        }
    }
}
