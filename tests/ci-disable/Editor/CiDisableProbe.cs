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
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Deliberately no MCP assembly reference: this probe must also compile when MCP does not.
public static class CiDisableProbe
{
    public static void Run()
    {
        try
        {
            var mode = Environment.GetEnvironmentVariable("UNITY_MCP_PROBE_MODE") ?? "normal";
            var assemblies = CompilationPipeline.GetAssemblies();
            var mcp = assemblies.Where(a => a.name.StartsWith("com.IvanMurzak.Unity.MCP.") && !a.name.Contains(".Installer")).ToArray();
            if (mode == "excluded")
            {
                Require(mcp.Length == 0, "Excluded MCP assemblies still in compilation: " + string.Join(", ", mcp.Select(a => a.name)));
                Require(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.StartsWith("com.IvanMurzak.Unity.MCP.") && !a.GetName().Name.Contains(".Installer")), "Excluded MCP assembly still loaded");
            }
            else
            {
                Require(mcp.Any(a => a.name == "com.IvanMurzak.Unity.MCP.Editor"), "Core editor assembly missing");
                Require(mcp.Any(a => a.name == "com.IvanMurzak.Unity.MCP.InputSystem.Editor"), "Extension control assembly missing");
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("com.IvanMurzak.Unity.MCP.UnityMcpPluginEditor")).First(t => t != null);
                var hasInstance = (bool)type.GetProperty("HasInstance").GetValue(null);
                Require(hasInstance == (mode == "normal"), "Unexpected editor singleton state for " + mode);
                if (mode == "disabled")
                {
                    var instance = type.GetProperty("Instance").GetValue(null);
                    type.GetMethod("BuildMcpPluginIfNeeded").Invoke(instance, null);
                    Require(type.GetProperty("McpPluginInstance").GetValue(instance) == null, "Explicit build bypassed disable");
                    Require(!Resources.FindObjectsOfTypeAll<MonoBehaviour>().Any(o => o.GetType().Name == "MainThreadDispatcher"), "MCP dispatcher created");
                }
            }
            Debug.Log("CI_DISABLE_PROBE_PASS " + mode);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(2);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
