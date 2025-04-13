#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

using System.Collections.Generic;
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ObjectUtils
    {
        public static T[] MakeArray<T>(this T item) => new T[] { item };
        public static List<T> MakeList<T>(this T item) => new List<T> { item };

        public static JsonElement ToJsonElement(this object obj)
        {
            // Serialize the object to a UTF-8 byte array
            var json = JsonSerializer.Serialize(obj);

            // Parse the JSON string into a JsonDocument and return the root element
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone(); // Clone to make it usable outside the 'using' block
        }
    }
}