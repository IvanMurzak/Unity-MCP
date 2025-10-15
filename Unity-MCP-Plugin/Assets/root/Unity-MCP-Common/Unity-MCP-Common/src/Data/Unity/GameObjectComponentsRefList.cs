/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Collections.Generic;
using System.ComponentModel;

namespace com.IvanMurzak.Unity.MCP.Common.Model.Unity
{
    [System.Serializable]
    [Description(@"GameObject references array. Used to specify GameObjects in opened Prefab or in the active Scene.")]
    public class GameObjectComponentsRefList : List<GameObjectComponentsRef>
    {
        public GameObjectComponentsRefList() { }

        public GameObjectComponentsRefList(int capacity) : base(capacity) { }

        public GameObjectComponentsRefList(IEnumerable<GameObjectComponentsRef> collection) : base(collection) { }

        public override string ToString()
        {
            if (Count == 0)
                return "No GameObjects";

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.AppendLine($"GameObjects total amount: {Count}");

            for (int i = 0; i < Count; i++)
                stringBuilder.AppendLine($"GameObject[{i}] {this[i]}");

            return stringBuilder.ToString();
        }
    }
}
