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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsRuntimeGameObjectRef
    {
        public static GameObject? FindGameObject(this GameObjectRef? objectRef)
            => FindGameObject(objectRef, out _);

        public static GameObject? FindGameObject(this GameObjectRef? objectRef, out string? error)
        {
            if (objectRef == null)
            {
                error = null;
                return null;
            }

            var go = GameObjectUtils.FindBy(objectRef, out error);
            if (go == null)
                go = ExtensionsRuntimeAssetObjectRef.FindAssetObject(objectRef) as GameObject;

            if (go != null)
                error = null;

            return go;
        }
        public static GameObjectRef? ToGameObjectRef(this GameObject? obj)
        {
            return new GameObjectRef(obj);
        }
    }
}
