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
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    public partial class UnityMcpPlugin
    {
        public const string Version = "0.21.0";

        static volatile object initializingMutex = new();
        static volatile Mutex initializedMutex = new();
        static volatile bool isInitializing = false;
        static volatile bool isInitialized = false;

        public static async void BuildAndStart(bool openConnectionIfNeeded = true)
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildAndStart));

            // Disable automatic connection in CI environments
            if (EnvironmentUtils.IsCi())
                openConnectionIfNeeded = false;

            lock (initializingMutex)
            {
                if (isInitializing)
                {
                    _logger.Log(MicrosoftLogLevel.Debug, "{tag} {class} is already in progress. Skipping this call.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin));
                    _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildAndStart));
                    return;
                }
                // Initialization started
                isInitializing = true;
            }

            initializedMutex.WaitOne();
            try
            {
                if (isInitialized)
                {
                    _logger.Log(MicrosoftLogLevel.Debug, "{tag} {class} is already initialized. Skipping this call.",
                        Consts.Log.Tag, nameof(UnityMcpPlugin));

                    if (openConnectionIfNeeded && KeepConnected)
                    {
                        if (!McpPlugin.HasInstance)
                        {
                            _logger.Log(MicrosoftLogLevel.Error, "{tag} {class} instance is null while isInitialized is true.",
                                Consts.Log.Tag, nameof(UnityMcpPlugin));

                            return;
                        }
                        await McpPlugin.Instance.Connect();
                    }
                    return;
                }

                await BuildAndStartInternal(openConnectionIfNeeded);

                isInitialized = true;
                _logger.Log(MicrosoftLogLevel.Debug, "{tag} {class} isInitialized set <true>.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin));
            }
            catch (Exception ex)
            {
                isInitialized = false;
                _logger.Log(MicrosoftLogLevel.Debug, "{tag} {class} isInitialized set <false>.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin));

                Debug.LogException(ex);
                _logger.Log(MicrosoftLogLevel.Error, "{tag} {class} Error during MCP plugin initialization: {exception}",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), ex);

                await McpPlugin.StaticDisposeAsync();
            }
            finally
            {
                _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildAndStart));
                initializedMutex.ReleaseMutex();
                lock (initializingMutex)
                {
                    isInitializing = false;
                }
            }
        }

        static async Task BuildAndStartInternal(bool openConnectionIfNeeded)
        {
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() called.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildAndStartInternal));

            if (McpPlugin.HasInstance)
            {
                _logger.Log(MicrosoftLogLevel.Error, "{tag} {class} instance already exists.",
                    Consts.Log.Tag, nameof(UnityMcpPlugin));
                return;
            }

            MainThreadInstaller.Init();

            var version = new Common.Version
            {
                Api = Consts.ApiVersion,
                Plugin = UnityMcpPlugin.Version,
                UnityVersion = Application.unityVersion
            };
            var loggerProvider = new UnityLoggerProvider();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var mcpPlugin = new McpPluginBuilder(version, loggerProvider)
                .AddMcpPlugin()
                .WithConfig(config =>
                {
                    _logger.Log(MicrosoftLogLevel.Information, "{tag} MCP server address: {host}",
                        Consts.Log.Tag, Host);

                    config.Endpoint = Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers
                    loggingBuilder.AddProvider(loggerProvider);
                    loggingBuilder.SetMinimumLevel(LogLevel switch
                    {
                        LogLevel.Trace => MicrosoftLogLevel.Trace,
                        LogLevel.Debug => MicrosoftLogLevel.Debug,
                        LogLevel.Info => MicrosoftLogLevel.Information,
                        LogLevel.Warning => MicrosoftLogLevel.Warning,
                        LogLevel.Error => MicrosoftLogLevel.Error,
                        LogLevel.Exception => MicrosoftLogLevel.Critical,
                        _ => MicrosoftLogLevel.Warning
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
                var message = "<b><color=yellow>Connecting</color></b>";
                _logger.Log(MicrosoftLogLevel.Information, "{tag} {message} <color=orange>ಠ‿ಠ</color>",
                    Consts.Log.Tag, message);
                await mcpPlugin.Connect();
            }
            _logger.Log(MicrosoftLogLevel.Trace, "{tag} {class}.{method}() completed.",
                Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(BuildAndStartInternal));
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
