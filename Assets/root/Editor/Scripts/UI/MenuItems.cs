#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static class MenuItems
    {
        [MenuItem("Tools/Unity-MCP/Build and Start", priority = 1000)]
        public static void BuildAndStart() => Startup.BuildAndStart();

        [MenuItem("Tools/Unity-MCP/Connect", priority = 1001)]
        public static void Editor_Connect() => Connector.Instance.Connect();

        [MenuItem("Tools/Unity-MCP/Disconnect", priority = 1001)]
        public static void Editor_Disconnect()
        {
            if (!Connector.HasInstance)
            {
                Debug.Log($"{Consts.Log.Tag} {Consts.Log.Color.CategoryStart}Connector{Consts.Log.Color.CategoryEnd} Already disconnected.");
                return;
            }
            Connector.Instance.Disconnect();
        }

        [MenuItem("Tools/Unity-MCP/Server/Build", priority = 1010)]
        public static void CompileServer() => Startup.CompileServer();

        [MenuItem("Tools/Unity-MCP/Server/Print Config", priority = 1011)]
        public static void PrintConfig()
        {
            var config = Consts.MCP_Client.ClaudeDesktop.Config.Replace("{0}", Startup.ServerExecutablePath.Replace('\\', '/'));
            Debug.Log($"{Consts.Log.Tag} Copy and paste this config to <color=orange>Claude Desktop</color> config.json");
            Debug.Log($"{Consts.Log.Tag} Server Config is RIGHT HERE:\n{config}");
        }
    }
}
#endif