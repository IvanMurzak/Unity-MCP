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
using com.IvanMurzak.Unity.MCP.Common.Model.Unity;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Utils
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

        /// <summary>
        /// Updates a JsonElement by setting or replacing a specific property with a new value.
        /// </summary>
        /// <param name="originalElement">The original JsonElement to update</param>
        /// <param name="propertyName">The name of the property to set/replace</param>
        /// <param name="newValue">The new value for the property</param>
        /// <returns>A new JsonElement with the updated property</returns>
        public static JsonElement SetProperty(
            this ref JsonElement? originalElement,
            string propertyName,
            int newValue)
        {
            // Check if need to set value
            if (originalElement != null && originalElement.Value.TryGetProperty(propertyName, out var propertyElement))
            {
                if (propertyElement.TryGetInt32(out var existedValue))
                {
                    if (existedValue == newValue)
                        return originalElement.Value; // no need to set value
                }
            }

            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            if (originalElement == null)
            {
                // If originalElement is null, we just write the new property
                writer.WriteNumber(propertyName, newValue);
            }
            else
            {
                // Copy all existing properties except the one we're updating
                foreach (var property in originalElement.Value.EnumerateObject())
                {
                    if (property.Name != propertyName)
                    {
                        property.WriteTo(writer);
                    }
                }
                // Write the new property value
                writer.WriteNumber(propertyName, newValue);
            }

            writer.WriteEndObject();
            writer.Flush();

            // Parse and return the new JsonElement
            var correctedJson = Encoding.UTF8.GetString(stream.ToArray());
            originalElement = JsonDocument.Parse(correctedJson).RootElement;
            return originalElement.Value;
        }

        /// <summary>
        /// Updates a JsonElement by setting or replacing a specific property with a new string value.
        /// </summary>
        /// <param name="originalElement">The original JsonElement to update</param>
        /// <param name="propertyName">The name of the property to set/replace</param>
        /// <param name="newValue">The new string value for the property</param>
        /// <returns>A new JsonElement with the updated property</returns>
        public static JsonElement SetProperty(
            this ref JsonElement? originalElement,
            string propertyName,
            string newValue)
        {
            // Check if need to set value
            if (originalElement != null && originalElement.Value.TryGetProperty(propertyName, out var propertyElement))
            {
                if (propertyElement.ValueKind == JsonValueKind.String)
                {
                    var existedValue = propertyElement.GetString();
                    if (existedValue == newValue)
                        return originalElement.Value; // no need to set value
                }
            }

            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            if (originalElement == null)
            {
                // If originalElement is null, we just write the new property
                writer.WriteString(propertyName, newValue);
            }
            else
            {
                // Copy all existing properties except the one we're updating
                foreach (var property in originalElement.Value.EnumerateObject())
                {
                    if (property.Name != propertyName)
                    {
                        property.WriteTo(writer);
                    }
                }
                // Write the new property value
                writer.WriteString(propertyName, newValue);
            }

            writer.WriteEndObject();
            writer.Flush();

            // Parse and return the new JsonElement
            var correctedJson = Encoding.UTF8.GetString(stream.ToArray());
            originalElement = JsonDocument.Parse(correctedJson).RootElement;
            return originalElement.Value;
        }

        /// <summary>
        /// Updates a JsonElement by setting or replacing a specific property with a new boolean value.
        /// </summary>
        /// <param name="originalElement">The original JsonElement to update</param>
        /// <param name="propertyName">The name of the property to set/replace</param>
        /// <param name="newValue">The new boolean value for the property</param>
        /// <returns>A new JsonElement with the updated property</returns>
        public static JsonElement SetProperty(
            this ref JsonElement? originalElement,
            string propertyName,
            bool newValue)
        {
            // Check if need to set value
            if (originalElement != null && originalElement.Value.TryGetProperty(propertyName, out var propertyElement))
            {
                if (propertyElement.ValueKind == JsonValueKind.True || propertyElement.ValueKind == JsonValueKind.False)
                {
                    var existedValue = propertyElement.GetBoolean();
                    if (existedValue == newValue)
                        return originalElement.Value; // no need to set value
                }
            }

            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            if (originalElement == null)
            {
                // If originalElement is null, we just write the new property
                writer.WriteBoolean(propertyName, newValue);
            }
            else
            {
                // Copy all existing properties except the one we're updating
                foreach (var property in originalElement.Value.EnumerateObject())
                {
                    if (property.Name != propertyName)
                    {
                        property.WriteTo(writer);
                    }
                }
                // Write the new property value
                writer.WriteBoolean(propertyName, newValue);
            }

            writer.WriteEndObject();
            writer.Flush();

            // Parse and return the new JsonElement
            var correctedJson = Encoding.UTF8.GetString(stream.ToArray());
            originalElement = JsonDocument.Parse(correctedJson).RootElement;
            return originalElement.Value;
        }

        /// <summary>
        /// Updates a JsonElement by setting or replacing a specific property with a new float value.
        /// </summary>
        /// <param name="originalElement">The original JsonElement to update</param>
        /// <param name="propertyName">The name of the property to set/replace</param>
        /// <param name="newValue">The new float value for the property</param>
        /// <returns>A new JsonElement with the updated property</returns>
        public static JsonElement SetProperty(
            this ref JsonElement? originalElement,
            string propertyName,
            float newValue)
        {
            // Check if need to set value
            if (originalElement != null && originalElement.Value.TryGetProperty(propertyName, out var propertyElement))
            {
                if (propertyElement.ValueKind == JsonValueKind.Number)
                {
                    if (propertyElement.TryGetSingle(out var existedValue))
                    {
                        if (System.Math.Abs(existedValue - newValue) < float.Epsilon)
                            return originalElement.Value; // no need to set value
                    }
                }
            }

            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            if (originalElement == null)
            {
                // If originalElement is null, we just write the new property
                writer.WriteNumber(propertyName, newValue);
            }
            else
            {
                // Copy all existing properties except the one we're updating
                foreach (var property in originalElement.Value.EnumerateObject())
                {
                    if (property.Name != propertyName)
                    {
                        property.WriteTo(writer);
                    }
                }
                // Write the new property value
                writer.WriteNumber(propertyName, newValue);
            }

            writer.WriteEndObject();
            writer.Flush();

            // Parse and return the new JsonElement
            var correctedJson = Encoding.UTF8.GetString(stream.ToArray());
            originalElement = JsonDocument.Parse(correctedJson).RootElement;
            return originalElement.Value;
        }
    }
}
