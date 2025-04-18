#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Assets
    {
        public static class Error
        {
            public static string NeitherProvided_AssetPath_AssetGuid()
                => $"[Error] Neither 'assetPath' or 'assetGuid' provided. Please provide at least one of them.";

            public static string NotFoundAsset(string assetPath, string assetGuid)
                => $"[Error] Asset not found. Path: '{assetPath}'. GUID: '{assetGuid}'.\n" +
                   $"Please check if the asset is in the project and the path is correct.";
        }
    }
}