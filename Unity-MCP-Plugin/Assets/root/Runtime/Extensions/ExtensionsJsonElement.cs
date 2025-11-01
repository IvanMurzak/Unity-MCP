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
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsJsonElement
    {
        public static GameObjectRef? ToGameObjectRef(
            this JsonElement? jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            return ToGameObjectRef(
                jsonElement: jsonElement.Value,
                reflector,
                suppressException,
                depth,
                stringBuilder,
                logger
            );
        }
        public static GameObjectRef? ToGameObjectRef(
            this JsonElement jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!suppressException)
                return JsonSerializer.Deserialize<GameObjectRef>(jsonElement, reflector.JsonSerializerOptions);
            try
            {
                return JsonSerializer.Deserialize<GameObjectRef>(jsonElement, reflector.JsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }
        public static ComponentRef? ToComponentRef(
            this JsonElement? jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            return ToComponentRef(
                jsonElement: jsonElement.Value,
                reflector,
                suppressException,
                depth,
                stringBuilder,
                logger
            );
        }
        public static ComponentRef? ToComponentRef(
            this JsonElement jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!suppressException)
                return JsonSerializer.Deserialize<ComponentRef>(jsonElement, reflector.JsonSerializerOptions);
            try
            {
                return JsonSerializer.Deserialize<ComponentRef>(jsonElement, reflector.JsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }
        public static AssetObjectRef? ToAssetObjectRef(
            this JsonElement? jsonElement,
            Reflector? reflector = null,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            return ToAssetObjectRef(
                jsonElement: jsonElement.Value,
                reflector,
                suppressException,
                depth,
                stringBuilder,
                logger
            );
        }
        public static AssetObjectRef? ToAssetObjectRef(
            this JsonElement jsonElement,
            Reflector? reflector = null,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!suppressException)
                return JsonSerializer.Deserialize<AssetObjectRef>(jsonElement, reflector?.JsonSerializerOptions);
            try
            {
                return JsonSerializer.Deserialize<AssetObjectRef>(jsonElement, reflector?.JsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }
        public static ObjectRef? ToObjectRef(
            this JsonElement? jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            return ToObjectRef(
                jsonElement: jsonElement.Value,
                reflector,
                suppressException,
                depth,
                stringBuilder,
                logger
            );
        }
        public static ObjectRef? ToObjectRef(
            this JsonElement jsonElement,
            Reflector reflector,
            bool suppressException = true,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!suppressException)
                return JsonSerializer.Deserialize<ObjectRef>(jsonElement, reflector.JsonSerializerOptions);
            try
            {
                return JsonSerializer.Deserialize<ObjectRef>(jsonElement, reflector.JsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
