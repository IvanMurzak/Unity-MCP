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
    /// Utility class to extract strongly typed method names from interface expressions.
    /// This provides compile-time safety for SignalR method names instead of using string constants.
    /// </summary>
    public static class SignalRMethodNames
    {
        /// <summary>
        /// Extracts the method name from a lambda expression for IMcpHubClient methods.
        /// </summary>
        /// <param name="expression">Lambda expression representing the method call</param>
        /// <returns>The method name as a string</returns>
        public static string GetClientMethodName<TResult>(Expression<System.Func<IMcpHubClient, TResult>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                return methodCall.Method.Name;
            }
            throw new System.ArgumentException("Expression must be a method call", nameof(expression));
        }

        /// <summary>
        /// Extracts the method name from a lambda expression for IMcpHubServer methods.
        /// </summary>
        /// <param name="expression">Lambda expression representing the method call</param>
        /// <returns>The method name as a string</returns>
        public static string GetServerMethodName<TResult>(Expression<System.Func<IMcpHubServer, TResult>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                return methodCall.Method.Name;
            }
            throw new System.ArgumentException("Expression must be a method call", nameof(expression));
        }

        /// <summary>
        /// Static class containing compile-time safe method names for client-side calls.
        /// </summary>
        public static class Client
        {
            public static readonly string RunCallTool = GetClientMethodName(c => c.RunCallTool(default!, default));
            public static readonly string RunListTool = GetClientMethodName(c => c.RunListTool(default!, default));
            public static readonly string RunResourceContent = GetClientMethodName(c => c.RunResourceContent(default!, default));
            public static readonly string RunListResources = GetClientMethodName(c => c.RunListResources(default!, default));
            public static readonly string RunListResourceTemplates = GetClientMethodName(c => c.RunListResourceTemplates(default!, default));
            public static readonly string ForceDisconnect = GetClientMethodName(c => c.ForceDisconnect());
        }

        /// <summary>
        /// Static class containing compile-time safe method names for server-side calls.
        /// </summary>
        public static class Server
        {
            public static readonly string OnListToolsUpdated = GetServerMethodName(s => s.OnListToolsUpdated(default!));
            public static readonly string OnListResourcesUpdated = GetServerMethodName(s => s.OnListResourcesUpdated(default!));
            public static readonly string OnToolRequestCompleted = GetServerMethodName(s => s.OnToolRequestCompleted(default!));
            public static readonly string OnVersionHandshake = GetServerMethodName(s => s.OnVersionHandshake(default!));
            public static readonly string OnDomainReloadStarted = GetServerMethodName(s => s.OnDomainReloadStarted());
            public static readonly string OnDomainReloadCompleted = GetServerMethodName(s => s.OnDomainReloadCompleted());
        }
    }
}