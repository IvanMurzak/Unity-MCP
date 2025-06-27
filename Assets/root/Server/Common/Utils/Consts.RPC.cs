#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class Consts
    {
        public static class Env
        {
            public const string Port = "UNITY_MCP_PORT";
            public const string Timeout = "UNITY_MCP_TIMEOUT";
        }
        public static class Hub
        {
            public const int DefaultPort = 60606;
            public const int MaxPort = 65535;
            public const string DefaultEndpoint = "http://localhost:60606";
            public const string RemoteApp = "/mcp/remote-app";
            public const int DefaultTimeoutMs = 10000;
        }

        public static partial class RPC
        {
            public static class Client
            {
                public const string RunCallTool = "/mcp/run-call-tool";
                public const string RunListTool = "/mcp/run-list-tool";
                public const string RunResourceContent = "/mcp/run-resource-content";
                public const string RunListResources = "/mcp/run-list-resources";
                public const string RunListResourceTemplates = "/mcp/run-list-resource-templates";

                public const string ForceDisconnect = "force-disconnect";
            }

            public static class Server
            {
                public const string OnListToolsUpdated = "OnListToolsUpdated";
                public const string OnListResourcesUpdated = "OnListResourcesUpdated";
            }
        }
    }
}