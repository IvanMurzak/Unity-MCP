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
    [Description(@"Component reference array. Used to find Component at GameObject.")]
    public class ComponentRefList : List<ComponentRef>
    {
        public ComponentRefList() { }

        public ComponentRefList(int capacity) : base(capacity) { }

        public ComponentRefList(IEnumerable<ComponentRef> collection) : base(collection) { }

        public override string ToString()
        {
            if (Count == 0)
                return "No Components";

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.AppendLine($"Components total amount: {Count}");

            for (int i = 0; i < Count; i++)
                stringBuilder.AppendLine($"Component[{i}] {this[i]}");

            return stringBuilder.ToString();
        }
    }
}
