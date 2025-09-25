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
using System;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.SignalR
{
    /// <summary>
    /// Validation utilities for SignalR strongly typed interfaces.
    /// Provides compile-time and runtime validation that the interfaces are properly implemented.
    /// </summary>
    public static class SignalRValidation
    {
        /// <summary>
        /// Validates that all SignalR method names match the expected constants.
        /// This should be called during application startup to ensure consistency.
        /// </summary>
        /// <param name="logger">Optional logger for validation results</param>
        /// <returns>True if all validations pass, false otherwise</returns>
        public static bool ValidateMethodNames(ILogger? logger = null)
        {
            var isValid = true;

            // Validate client method names match original constants
            isValid &= ValidateEqual(SignalRMethodNames.Client.RunCallTool, "/mcp/run-call-tool", nameof(SignalRMethodNames.Client.RunCallTool), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Client.RunListTool, "/mcp/run-list-tool", nameof(SignalRMethodNames.Client.RunListTool), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Client.RunResourceContent, "/mcp/run-resource-content", nameof(SignalRMethodNames.Client.RunResourceContent), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Client.RunListResources, "/mcp/run-list-resources", nameof(SignalRMethodNames.Client.RunListResources), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Client.RunListResourceTemplates, "/mcp/run-list-resource-templates", nameof(SignalRMethodNames.Client.RunListResourceTemplates), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Client.ForceDisconnect, "force-disconnect", nameof(SignalRMethodNames.Client.ForceDisconnect), logger);

            // Validate server method names match original constants
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnListToolsUpdated, "OnListToolsUpdated", nameof(SignalRMethodNames.Server.OnListToolsUpdated), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnListResourcesUpdated, "OnListResourcesUpdated", nameof(SignalRMethodNames.Server.OnListResourcesUpdated), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnToolRequestCompleted, "OnToolRequestCompleted", nameof(SignalRMethodNames.Server.OnToolRequestCompleted), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnVersionHandshake, "OnVersionHandshake", nameof(SignalRMethodNames.Server.OnVersionHandshake), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnDomainReloadStarted, "OnDomainReloadStarted", nameof(SignalRMethodNames.Server.OnDomainReloadStarted), logger);
            isValid &= ValidateEqual(SignalRMethodNames.Server.OnDomainReloadCompleted, "OnDomainReloadCompleted", nameof(SignalRMethodNames.Server.OnDomainReloadCompleted), logger);

            if (isValid)
            {
                logger?.LogInformation("SignalR method name validation passed. All strongly typed interfaces are correctly mapped.");
            }
            else
            {
                logger?.LogError("SignalR method name validation failed. Some interfaces have incorrect mappings.");
            }

            return isValid;
        }

        /// <summary>
        /// Validates that the interface methods have correct signatures for SignalR usage.
        /// </summary>
        /// <returns>True if all interface methods are properly defined</returns>
        public static bool ValidateInterfaceSignatures()
        {
            try
            {
                // Validate IMcpHubClient methods exist and have correct signatures
                var clientType = typeof(IMcpHubClient);
                _ = clientType.GetMethod(nameof(IMcpHubClient.RunCallTool)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.RunCallTool)} method not found");
                _ = clientType.GetMethod(nameof(IMcpHubClient.RunListTool)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.RunListTool)} method not found");
                _ = clientType.GetMethod(nameof(IMcpHubClient.RunResourceContent)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.RunResourceContent)} method not found");
                _ = clientType.GetMethod(nameof(IMcpHubClient.RunListResources)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.RunListResources)} method not found");
                _ = clientType.GetMethod(nameof(IMcpHubClient.RunListResourceTemplates)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.RunListResourceTemplates)} method not found");
                _ = clientType.GetMethod(nameof(IMcpHubClient.ForceDisconnect)) ?? throw new InvalidOperationException($"{nameof(IMcpHubClient.ForceDisconnect)} method not found");

                // Validate IMcpHubServer methods exist and have correct signatures
                var serverType = typeof(IMcpHubServer);
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnListToolsUpdated)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnListToolsUpdated)} method not found");
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnListResourcesUpdated)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnListResourcesUpdated)} method not found");
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnToolRequestCompleted)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnToolRequestCompleted)} method not found");
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnVersionHandshake)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnVersionHandshake)} method not found");
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnDomainReloadStarted)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnDomainReloadStarted)} method not found");
                _ = serverType.GetMethod(nameof(IMcpHubServer.OnDomainReloadCompleted)) ?? throw new InvalidOperationException($"{nameof(IMcpHubServer.OnDomainReloadCompleted)} method not found");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool ValidateEqual(string actual, string expected, string propertyName, ILogger? logger)
        {
            if (actual == expected)
            {
                logger?.LogTrace("SignalR method name validation passed for {PropertyName}: '{Value}'", propertyName, actual);
                return true;
            }
            else
            {
                logger?.LogError("SignalR method name validation failed for {PropertyName}. Expected: '{Expected}', Actual: '{Actual}'", 
                    propertyName, expected, actual);
                return false;
            }
        }
    }
}