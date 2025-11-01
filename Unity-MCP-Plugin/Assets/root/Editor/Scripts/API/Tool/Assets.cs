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
using com.IvanMurzak.McpPlugin.Common;

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

            public static string SourceOrDestinationPathIsEmpty()
                => "[Error] Source or destination path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourcePathsArrayIsEmpty()
                => "[Error] Source paths array is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourcePathIsEmpty()
                => "[Error] Source path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourceAndDestinationPathsArrayMustBeOfTheSameLength()
                => "[Error] Source and destination paths arrays must be of the same length.";

            public static string NotAllowedToModifyAssetInPackages(string assetPath)
                => $"[Error] Not allowed to modify asset in '/Packages' folder. Please modify it in '/Assets' folder. Path: '{assetPath}'.";

            public static string EmptyAssetPath()
                => "[Error] Asset path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
            public static string AssetPathMustStartWithAssets(string assetPath)
                => $"[Error] Asset path must start with 'Assets/'. Path: '{assetPath}'.";
            public static string AssetPathMustEndWithMat(string assetPath)
                => $"[Error] Asset path must end with '.mat'. Path: '{assetPath}'.";
            public static string ShaderNotFound(string shaderName)
                => $"[Error] Shader not found. Name: '{shaderName}'. Please check if the shader is in the project and the name is correct.";

            // public static string MaterialsPrinted => string.Join("\n", AssetDatabase.FindAssets("t:Material"));
        }
    }
}