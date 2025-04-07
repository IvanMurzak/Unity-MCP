#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using com.IvanMurzak.Unity.MCP.Common.Data;
using System.Text.Json;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class JsonUtils
    {
        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        };

        public static string ToJson(this IRequestData? data, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return "{}";

            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static string ToJson(this IResponseData? data, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return "{}";

            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static IRequestData? ParseRequestData(this string json, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<RequestData>(json, options ?? jsonSerializerOptions);
        }

        public static IResponseData? ParseResponseData(this string json, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<ResponseData>(json, options ?? jsonSerializerOptions);
        }
    }
}