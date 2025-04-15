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
                ));

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: "Unknonwn");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ));
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
                ));

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: "Unknonwn");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ));
        }

        void ConfigureClient(VisualElement root, string configPath)
        {
            var statusCircle = root.Query<VisualElement>("configureStatusCircle").First();
            var statusText = root.Query<Label>("configureStatusText").First();
            var btnConfigure = root.Query<Button>("btnConfigure").First();

            var isConfiguredResult = IsMcpClientConfigured(configPath);

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
                var configureResult = ConfigureMcpClient(configPath);

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

        bool IsMcpClientConfigured(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var json = File.ReadAllText(configPath);

                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    return false;

                var mcpServers = rootObj["mcpServers"]?.AsObject();
                if (mcpServers == null)
                    return false;

                foreach (var kv in mcpServers)
                {
                    var command = kv.Value?["command"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(command))
                    {
                        // Normalize both paths for comparison
                        try
                        {
                            var normalizedCommand = Path.GetFullPath(command.Replace('/', Path.DirectorySeparatorChar));
                            var normalizedTarget = Path.GetFullPath(Startup.ServerExecutableFile.Replace('/', Path.DirectorySeparatorChar));
                            if (string.Equals(normalizedCommand, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                        catch
                        {
                            // If normalization fails, fallback to string comparison
                            if (string.Equals(command, Startup.ServerExecutableFile, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                    }
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
        bool ConfigureMcpClient(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var json = File.ReadAllText(configPath);

                // Parse the existing config as JsonObject
                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    throw new Exception("Config file is not a valid JSON object.");

                // Parse the injected config as JsonObject
                var injectObj = JsonNode.Parse(Startup.RawJsonConfiguration)?.AsObject();
                if (injectObj == null)
                    throw new Exception("Injected config is not a valid JSON object.");

                // Get mcpServers from both
                var mcpServers = rootObj["mcpServers"]?.AsObject();
                var injectMcpServers = injectObj["mcpServers"]?.AsObject();
                if (mcpServers == null || injectMcpServers == null)
                    throw new Exception("Missing 'mcpServers' object in config.");

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

                return IsMcpClientConfigured(configPath);
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