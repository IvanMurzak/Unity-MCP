/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Convertor;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Json;
using com.IvanMurzak.Unity.MCP.Common.Json.Converters;
using com.IvanMurzak.Unity.MCP.Common.Reflection.Convertor;
using com.IvanMurzak.Unity.MCP.Reflection.Convertor;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    using Consts = Common.Consts;
    using LogLevel = Utils.LogLevel;
    using LogLevelMicrosoft = Microsoft.Extensions.Logging.LogLevel;

    public partial class McpPluginUnity
    {
        public const string Version = "0.17.2";

        static volatile object initializingMutex = new();
        static volatile Mutex initializedMutex = new();
        static volatile bool isInitializing = false;
        static volatile bool isInitialized = false;

        public static async void BuildAndStart(bool openConnectionIfNeeded = true)
        {
            if (IsLogActive(LogLevel.Trace))
                Debug.Log($"{Consts.Log.Tag} McpPluginUnity.BuildAndStart() called.");

            // Disable automatic connection in CI environments
            if (EnvironmentUtils.IsCi())
                openConnectionIfNeeded = false;

            lock (initializingMutex)
            {
                if (isInitializing)
                {
                    if (IsLogActive(LogLevel.Debug))
                        Debug.Log($"{Consts.Log.Tag} McpPluginUnity is already in progress. Skipping this call.");
                    if (IsLogActive(LogLevel.Trace))
                        Debug.Log($"{Consts.Log.Tag} McpPluginUnity.BuildAndStart() completed.");
                    return;
                }
                isInitializing = true;
            }

            initializedMutex.WaitOne();
            try
            {
                if (isInitialized)
                {
                    if (IsLogActive(LogLevel.Debug))
                        Debug.Log($"{Consts.Log.Tag} McpPluginUnity is already initialized. Skipping this call.");

                    if (openConnectionIfNeeded && KeepConnected)
                    {
                        if (!McpPlugin.HasInstance)
                        {
                            if (IsLogActive(LogLevel.Error))
                                Debug.LogError($"{Consts.Log.Tag} McpPluginUnity instance is null while isInitialized is true.");

                            return;
                        }
                        await McpPlugin.Instance.Connect();
                    }
                    return;
                }

                await BuildAndStartInternal(openConnectionIfNeeded);

                isInitialized = true;
                if (IsLogActive(LogLevel.Debug))
                    Debug.Log($"{Consts.Log.Tag} McpPluginUnity isInitialized set <true>.");
            }
            catch (Exception ex)
            {
                isInitialized = false;
                if (IsLogActive(LogLevel.Debug))
                    Debug.Log($"{Consts.Log.Tag} McpPluginUnity isInitialized set <false>.");

                Debug.LogException(ex);
                Debug.LogError($"{Consts.Log.Tag} Error during MCP plugin initialization: {ex}");

                McpPlugin.StaticDisposeAsync().GetAwaiter();
            }
            finally
            {
                if (IsLogActive(LogLevel.Trace))
                    Debug.Log($"{Consts.Log.Tag} McpPluginUnity.BuildAndStart() completed.");
                initializedMutex.ReleaseMutex();
                lock (initializingMutex)
                {
                    isInitializing = false;
                }
            }
        }

        static async Task BuildAndStartInternal(bool openConnectionIfNeeded)
        {
            if (IsLogActive(LogLevel.Trace))
                Debug.Log($"{Consts.Log.Tag} {nameof(McpPluginUnity)}.{nameof(BuildAndStartInternal)}() called.");

            if (McpPlugin.HasInstance)
            {
                if (IsLogActive(LogLevel.Error))
                    Debug.LogError($"{Consts.Log.Tag} {nameof(McpPluginUnity)} instance already exists.");
                return;
            }

            MainThreadInstaller.Init();

            var version = new Common.Version
            {
                Api = Consts.ApiVersion,
                Plugin = McpPluginUnity.Version,
                UnityVersion = Application.unityVersion
            };
            var loggerProvider = new UnityLoggerProvider();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var mcpPlugin = new McpPluginBuilder(version, loggerProvider)
                .AddMcpPlugin()
                .WithConfig(config =>
                {
                    if (IsLogActive(LogLevel.Info))
                        Debug.Log($"{Consts.Log.Tag} MCP server address: {Host}");

                    config.Endpoint = Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers
                    loggingBuilder.AddProvider(loggerProvider);
                    loggingBuilder.SetMinimumLevel(LogLevel switch
                    {
                        LogLevel.Trace => LogLevelMicrosoft.Trace,
                        LogLevel.Debug => LogLevelMicrosoft.Debug,
                        LogLevel.Info => LogLevelMicrosoft.Information,
                        LogLevel.Warning => LogLevelMicrosoft.Warning,
                        LogLevel.Error => LogLevelMicrosoft.Error,
                        LogLevel.Exception => LogLevelMicrosoft.Critical,
                        _ => LogLevelMicrosoft.Warning
                    });
                })
                .WithToolsFromAssembly(assemblies)
                .WithPromptsFromAssembly(assemblies)
                .WithResourcesFromAssembly(assemblies)
                .Build(CreateDefaultReflector());

            if (!openConnectionIfNeeded)
                return;

            if (KeepConnected)
            {
                if (IsLogActive(LogLevel.Info))
                {
                    var message = "<b><color=yellow>Connecting</color></b>";
                    Debug.Log($"{Consts.Log.Tag} {message} <color=orange>ಠ‿ಠ</color>");
                }
                await mcpPlugin.Connect();
            }
            if (IsLogActive(LogLevel.Trace))
                Debug.Log($"{Consts.Log.Tag} {nameof(McpPluginUnity)}.{nameof(BuildAndStartInternal)}() completed.");
        }

        static Reflector CreateDefaultReflector()
        {
            var reflector = new Reflector();

            // Remove converters that are not needed in Unity
            reflector.Convertors.Remove<GenericReflectionConvertor<object>>();
            reflector.Convertors.Remove<ArrayReflectionConvertor>();

            // Add Unity-specific converters
            reflector.Convertors.Add(new UnityGenericReflectionConvertor<object>());
            reflector.Convertors.Add(new UnityArrayReflectionConvertor());

            // Unity types
            reflector.Convertors.Add(new UnityEngine_Color32_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Color_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Matrix4x4_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Quaternion_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Vector2_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Vector2Int_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Vector3_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Vector3Int_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Vector4_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Bounds_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_BoundsInt_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Rect_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_RectInt_ReflectionConvertor());

            // Components
            reflector.Convertors.Add(new UnityEngine_Object_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_GameObject_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Component_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Transform_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Renderer_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_MeshFilter_ReflectionConvertor());

            // Assets
            reflector.Convertors.Add(new UnityEngine_Material_ReflectionConvertor());
            reflector.Convertors.Add(new UnityEngine_Sprite_ReflectionConvertor());

            // Json Converters
            // ---------------------------------------------------------

            // Unity types
            reflector.JsonSerializer.AddConverter(new Color32Converter());
            reflector.JsonSerializer.AddConverter(new ColorConverter());
            reflector.JsonSerializer.AddConverter(new Matrix4x4Converter());
            reflector.JsonSerializer.AddConverter(new QuaternionConverter());
            reflector.JsonSerializer.AddConverter(new Vector2Converter());
            reflector.JsonSerializer.AddConverter(new Vector2IntConverter());
            reflector.JsonSerializer.AddConverter(new Vector3Converter());
            reflector.JsonSerializer.AddConverter(new Vector3IntConverter());
            reflector.JsonSerializer.AddConverter(new Vector4Converter());
            reflector.JsonSerializer.AddConverter(new BoundsConverter());
            reflector.JsonSerializer.AddConverter(new BoundsIntConverter());
            reflector.JsonSerializer.AddConverter(new RectConverter());
            reflector.JsonSerializer.AddConverter(new RectIntConverter());

            // Reference types
            reflector.JsonSerializer.AddConverter(new ObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new AssetObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new GameObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new ComponentRefConverter());

            return reflector;
        }
    }
}
