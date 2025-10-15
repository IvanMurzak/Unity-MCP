using com.IvanMurzak.ReflectorNet;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class RpcJsonConfiguration
    {
        public static void ConfigureJsonSerializer(Reflector reflector, Microsoft.AspNetCore.SignalR.JsonHubProtocolOptions options)
        {
            var jsonSerializerOptions = reflector.JsonSerializer.JsonSerializerOptions;

            options.PayloadSerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
            options.PayloadSerializerOptions.PropertyNamingPolicy = jsonSerializerOptions.PropertyNamingPolicy;
            options.PayloadSerializerOptions.WriteIndented = jsonSerializerOptions.WriteIndented;

            foreach (var converter in jsonSerializerOptions.Converters)
                options.PayloadSerializerOptions.Converters.Add(converter);
        }
    }
}