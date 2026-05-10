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
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Assets
    {
        public static class Error
        {
            public static string NeitherProvided_AssetPath_AssetGuid()
                => $"Neither 'assetPath' or 'assetGuid' provided. Please provide at least one of them.";

            public static string NotFoundAsset(string assetPath, string assetGuid)
                => $"Asset not found. Path: '{assetPath}'. GUID: '{assetGuid}'.\n" +
                   $"Please check if the asset is in the project and the path is correct.";

            public static string SourceOrDestinationPathIsEmpty()
                => "Source or destination path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourcePathsArrayIsEmpty()
                => "Source paths array is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourcePathIsEmpty()
                => "Source path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";

            public static string SourceAndDestinationPathsArrayMustBeOfTheSameLength()
                => "Source and destination paths arrays must be of the same length.";

            public static string NotAllowedToModifyAssetInPackages(string assetPath)
                => $"Not allowed to modify asset in '/Packages' folder. Please modify it in '/Assets' folder. Path: '{assetPath}'.";

            public static string EmptyAssetPath()
                => "Asset path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
            public static string AssetPathMustStartWithAssets(string assetPath)
                => $"Asset path must start with 'Assets/'. Path: '{assetPath}'.";
            public static string AssetPathMustEndWithMat(string assetPath)
                => $"Asset path must end with '.mat'. Path: '{assetPath}'.";
            public static string ShaderNotFound(string shaderName)
                => $"Shader not found. Name: '{shaderName}'. Please check if the shader is in the project and the name is correct.";

            // public static string MaterialsPrinted => string.Join("\n", AssetDatabase.FindAssets("t:Material"));
        }
    }
}