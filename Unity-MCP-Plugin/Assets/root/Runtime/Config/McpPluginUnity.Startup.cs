using System;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Convertor;
using com.IvanMurzak.ReflectorNet.Utils;
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
    using LogLevelMicrosoft = Microsoft.Extensions.Logging.LogLevel;
    using LogLevel = Utils.LogLevel;
    using Consts = Common.Consts;

    public partial class McpPluginUnity
    {
        public static void BuildAndStart(bool openConnection = true)
        {
            McpPlugin.StaticDisposeAsync();
            MainThreadInstaller.Init();

            var mcpPlugin = new McpPluginBuilder()
                .WithAppFeatures()
                .WithConfig(config =>
                {
                    if (McpPluginUnity.LogLevel.IsActive(LogLevel.Info))
                        Debug.Log($"{Consts.Log.Tag} MCP server address: {McpPluginUnity.Host}");

                    config.Endpoint = McpPluginUnity.Host;
                })
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // 👈 Clears the default providers
                    loggingBuilder.AddProvider(new UnityLoggerProvider());
                    loggingBuilder.SetMinimumLevel(McpPluginUnity.LogLevel switch
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
                .WithToolsFromAssembly(AppDomain.CurrentDomain.GetAssemblies())
                .WithPromptsFromAssembly(AppDomain.CurrentDomain.GetAssemblies())
                .WithResourcesFromAssembly(AppDomain.CurrentDomain.GetAssemblies())
                .Build(CreateDefaultReflector());

            if (!openConnection)
                return;

            if (McpPluginUnity.KeepConnected)
            {
                if (McpPluginUnity.LogLevel.IsActive(LogLevel.Info))
                {
                    var message = "<b><color=yellow>Connecting</color></b>";
                    Debug.Log($"{Consts.Log.Tag} {message} <color=orange>ಠ‿ಠ</color>");
                }
                mcpPlugin.Connect();
            }
        }

        static Reflector CreateDefaultReflector()
        {
            var reflector = new Reflector();

            // Remove converters that are not needed in Unity
            reflector.Convertors.Remove<GenericReflectionConvertor<object>>();
            reflector.Convertors.Remove<ArrayReflectionConvertor>();

            // Add Unity-specific converters
            reflector.Convertors.Add(new RS_GenericUnity<object>());
            reflector.Convertors.Add(new RS_ArrayUnity());

            // Unity types
            reflector.Convertors.Add(new RS_UnityEngineColor32());
            reflector.Convertors.Add(new RS_UnityEngineColor());
            reflector.Convertors.Add(new RS_UnityEngineMatrix4x4());
            reflector.Convertors.Add(new RS_UnityEngineQuaternion());
            reflector.Convertors.Add(new RS_UnityEngineVector2());
            reflector.Convertors.Add(new RS_UnityEngineVector2Int());
            reflector.Convertors.Add(new RS_UnityEngineVector3());
            reflector.Convertors.Add(new RS_UnityEngineVector3Int());
            reflector.Convertors.Add(new RS_UnityEngineVector4());
            reflector.Convertors.Add(new RS_UnityEngineBounds());
            reflector.Convertors.Add(new RS_UnityEngineBoundsInt());
            reflector.Convertors.Add(new RS_UnityEngineRect());
            reflector.Convertors.Add(new RS_UnityEngineRectInt());

            // Components
            reflector.Convertors.Add(new RS_UnityEngineObject());
            reflector.Convertors.Add(new RS_UnityEngineGameObject());
            reflector.Convertors.Add(new RS_UnityEngineComponent());
            reflector.Convertors.Add(new RS_UnityEngineTransform());
            reflector.Convertors.Add(new RS_UnityEngineRenderer());
            reflector.Convertors.Add(new RS_UnityEngineMeshFilter());

            // Assets
            reflector.Convertors.Add(new RS_UnityEngineMaterial());
            reflector.Convertors.Add(new RS_UnityEngineSprite());

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