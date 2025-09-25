/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Linq.Expressions;
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Common.SignalR
{
    /// <summary>
    /// Utility class that provides strongly typed method names for SignalR calls.
    /// This provides compile-time safety while using the original route/method names that were defined in constants.
    /// The interfaces ensure type safety, while this class maps to the actual SignalR routes.
    /// </summary>
    public static class SignalRMethodNames
    {
        /// <summary>
        /// Static class containing compile-time safe method names for client-side calls.
        /// These match the original constants from Consts.RPC.Client.
        /// </summary>
        public static class Client
        {
            // Original route-based names for client methods (server calls these on client)
            public static readonly string RunCallTool = "/mcp/run-call-tool";
            public static readonly string RunListTool = "/mcp/run-list-tool";
            public static readonly string RunResourceContent = "/mcp/run-resource-content";
            public static readonly string RunListResources = "/mcp/run-list-resources";
            public static readonly string RunListResourceTemplates = "/mcp/run-list-resource-templates";
            public static readonly string ForceDisconnect = "force-disconnect";
        }

        /// <summary>
        /// Static class containing compile-time safe method names for server-side calls.
        /// These match the original constants from Consts.RPC.Server.
        /// </summary>
        public static class Server
        {
            // Original method names for server methods (client calls these on server)
            public static readonly string OnListToolsUpdated = "OnListToolsUpdated";
            public static readonly string OnListResourcesUpdated = "OnListResourcesUpdated";
            public static readonly string OnToolRequestCompleted = "OnToolRequestCompleted";
            public static readonly string OnVersionHandshake = "OnVersionHandshake";
            public static readonly string OnDomainReloadStarted = "OnDomainReloadStarted";
            public static readonly string OnDomainReloadCompleted = "OnDomainReloadCompleted";
        }

        /// <summary>
        /// Validates that an interface method corresponds to a known SignalR method.
        /// This provides compile-time checking that the interface methods are properly mapped.
        /// </summary>
        /// <param name="expression">Lambda expression representing the method call</param>
        /// <returns>True if the method is valid for SignalR</returns>
        public static bool IsValidClientMethod<TResult>(Expression<System.Func<IMcpHubClient, TResult>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;
                return methodName switch
                {
                    nameof(IMcpHubClient.RunCallTool) => true,
                    nameof(IMcpHubClient.RunListTool) => true,
                    nameof(IMcpHubClient.RunResourceContent) => true,
                    nameof(IMcpHubClient.RunListResources) => true,
                    nameof(IMcpHubClient.RunListResourceTemplates) => true,
                    nameof(IMcpHubClient.ForceDisconnect) => true,
                    _ => false
                };
            }
            return false;
        }

        /// <summary>
        /// Validates that an interface method corresponds to a known SignalR method.
        /// </summary>
        /// <param name="expression">Lambda expression representing the method call</param>
        /// <returns>True if the method is valid for SignalR</returns>
        public static bool IsValidServerMethod<TResult>(Expression<System.Func<IMcpHubServer, TResult>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;
                return methodName switch
                {
                    nameof(IMcpHubServer.OnListToolsUpdated) => true,
                    nameof(IMcpHubServer.OnListResourcesUpdated) => true,
                    nameof(IMcpHubServer.OnToolRequestCompleted) => true,
                    nameof(IMcpHubServer.OnVersionHandshake) => true,
                    nameof(IMcpHubServer.OnDomainReloadStarted) => true,
                    nameof(IMcpHubServer.OnDomainReloadCompleted) => true,
                    _ => false
                };
            }
            return false;
        }
    }
}