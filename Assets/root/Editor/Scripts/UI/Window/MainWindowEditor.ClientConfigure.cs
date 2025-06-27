using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public partial class MainWindowEditor : EditorWindow
    {
        void ConfigureClientsWindows(VisualElement root)
        {
            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Claude",
                    "claude_desktop_config.json"
                ),
                bodyName: "mcpServers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: Path.Combine(
                    ".vscode",
                    "mcp.json"
                ),
                bodyName: "servers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ),
                bodyName: "mcpServers");
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    "Claude",
                    "claude_desktop_config.json"
                ),
                bodyName: "mcpServers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: Path.Combine(
                    ".vscode",
                    "mcp.json"
                ),
                bodyName: "servers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ),
                bodyName: "mcpServers");
        }

        void ConfigureClient(VisualElement root, string configPath, string bodyName = "mcpServers")
        {
            var statusCircle = root.Query<VisualElement>("configureStatusCircle").First();
            var statusText = root.Query<Label>("configureStatusText").First();
            var btnConfigure = root.Query<Button>("btnConfigure").First();

            var isConfiguredResult = IsMcpClientConfigured(configPath, bodyName);

            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

            statusCircle.AddToClassList(isConfiguredResult
                ? USS_IndicatorClass_Connected
                : USS_IndicatorClass_Disconnected);

            statusText.text = isConfiguredResult ? "Configured" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = ConfigureMcpClient(configPath, bodyName);

                statusText.text = configureResult ? "Configured" : "Not Configured";

                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

                statusCircle.AddToClassList(configureResult
                    ? USS_IndicatorClass_Connected
                    : USS_IndicatorClass_Disconnected);

                btnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
        }

        bool IsMcpClientConfigured(string configPath, string bodyName = "mcpServers")
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var json = File.ReadAllText(configPath);

                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    return false;

                var mcpServers = rootObj[bodyName]?.AsObject();
                if (mcpServers == null)
                    return false;

                foreach (var kv in mcpServers)
                {
                    var command = kv.Value?["command"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(command) || !IsCommandMatch(command))
                        continue;

                    var args = kv.Value?["args"]?.AsArray();
                    return DoArgumentsMatch(args);
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        bool IsCommandMatch(string command)
        {
            // Normalize both paths for comparison
            try
            {
                var normalizedCommand = Path.GetFullPath(command.Replace('/', Path.DirectorySeparatorChar));
                var normalizedTarget = Path.GetFullPath(Startup.ServerExecutableFile.Replace('/', Path.DirectorySeparatorChar));
                return string.Equals(normalizedCommand, normalizedTarget, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If normalization fails, fallback to string comparison
                return string.Equals(command, Startup.ServerExecutableFile, StringComparison.OrdinalIgnoreCase);
            }
        }

        bool DoArgumentsMatch(JsonArray args)
        {
            if (args == null)
                return false;

            var targetPort = McpPluginUnity.Port.ToString();
            var targetTimeout = McpPluginUnity.TimeoutMs.ToString();
            
            var foundPort = false;
            var foundTimeout = false;

            // Check for both positional and named argument formats
            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i]?.GetValue<string>();
                if (string.IsNullOrEmpty(arg)) 
                    continue;

                // Check positional format
                if (i == 0 && arg == targetPort)
                    foundPort = true;
                else if (i == 1 && arg == targetTimeout)
                    foundTimeout = true;
                
                // Check named format
                else if (arg.StartsWith("--port=") && arg.Substring(7) == targetPort)
                    foundPort = true;
                else if (arg.StartsWith("--timeout=") && arg.Substring(10) == targetTimeout)
                    foundTimeout = true;
            }

            return foundPort && foundTimeout;
        }

        bool ConfigureMcpClient(string configPath, string bodyName = "mcpServers")
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            try
            {
                if (!File.Exists(configPath))
                {
                    // Create the file if it doesn't exist
                    File.WriteAllText(configPath, Startup.RawJsonConfiguration(McpPluginUnity.Port, bodyName, McpPluginUnity.TimeoutMs));
                    return true;
                }

                var json = File.ReadAllText(configPath);

                // Parse the existing config as JsonObject
                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    throw new Exception("Config file is not a valid JSON object.");

                // Parse the injected config as JsonObject
                var injectObj = JsonNode.Parse(Startup.RawJsonConfiguration(McpPluginUnity.Port, bodyName, McpPluginUnity.TimeoutMs))?.AsObject();
                if (injectObj == null)
                    throw new Exception("Injected config is not a valid JSON object.");

                // Get mcpServers from both
                var mcpServers = rootObj[bodyName]?.AsObject();
                var injectMcpServers = injectObj[bodyName]?.AsObject();
                if (injectMcpServers == null)
                    throw new Exception($"Missing '{bodyName}' object in config.");

                if (mcpServers == null)
                {
                    // If mcpServers is null, create it
                    rootObj[bodyName] = JsonNode.Parse(injectMcpServers.ToJsonString())?.AsObject();
                    File.WriteAllText(configPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                    return IsMcpClientConfigured(configPath, bodyName);
                }

                // Find all command values in injectMcpServers
                var injectCommands = injectMcpServers
                    .Select(kv => kv.Value?["command"]?.GetValue<string>())
                    .Where(cmd => !string.IsNullOrEmpty(cmd))
                    .ToHashSet();

                // Remove any entry in mcpServers with a matching command
                var keysToRemove = mcpServers
                    .Where(kv => injectCommands.Contains(kv.Value?["command"]?.GetValue<string>()))
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                    mcpServers.Remove(key);

                // Merge/overwrite entries from injectMcpServers
                foreach (var kv in injectMcpServers)
                {
                    // Clone the value to avoid parent conflict
                    mcpServers[kv.Key] = kv.Value?.ToJsonString() is string jsonStr
                        ? JsonNode.Parse(jsonStr)
                        : null;
                }

                // Write back to file
                File.WriteAllText(configPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                return IsMcpClientConfigured(configPath, bodyName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }
    }
}