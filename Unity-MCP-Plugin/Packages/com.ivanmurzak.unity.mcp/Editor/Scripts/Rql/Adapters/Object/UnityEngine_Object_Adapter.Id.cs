/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if UNITY_6000_5_OR_NEWER
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    public sealed partial class UnityEngine_Object_Adapter
    {
        // Unity 6.5+ (current): the raw EntityId. EntityId.ToString() is "index:version" there and does not round-trip.
        private static string IdOf(Object o) => EntityId.ToULong(o.GetEntityId()).ToString(CultureInfo.InvariantCulture);
    }
}
#endif
