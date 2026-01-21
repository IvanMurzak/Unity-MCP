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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsCreateFolderToolId = "assets-create-folder";
        [McpPluginTool
        (
            AssetsCreateFolderToolId,
            Title = "Assets / Create Folder"
        )]
        [Description("Creates a new folder in the specified parent folder. " +
            "The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. " +
            "For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. " +
            "Use it to organize scripts and assets in the project. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Returns the GUID of the newly created folder, if successful.")]
        public CreateFolderResponse CreateFolders
        (
            [Description("The paths for the folders to create.")]
            CreateFolderInput[] inputs
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (inputs.Length == 0)
                    throw new System.Exception("The input array is empty.");

                var response = new CreateFolderResponse();

                foreach (var input in inputs)
                {
                    var guid = AssetDatabase.CreateFolder(input.ParentFolderPath, input.NewFolderName);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        response.CreatedFolderGuids ??= new();
                        response.CreatedFolderGuids.Add(guid);
                    }
                    else
                    {
                        response.Errors ??= new();
                        response.Errors.Add($"Failed to create folder '{input.NewFolderName}' in '{input.ParentFolderPath}'.");
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();

                return response;
            });
        }

        public class CreateFolderInput
        {
            [Description("The parent folder path where the new folder will be created.")]
            public string ParentFolderPath { get; set; } = string.Empty;
            [Description("The name of the new folder to create.")]
            public string NewFolderName { get; set; } = string.Empty;
        }
        public class CreateFolderResponse
        {
            [Description("List of GUIDs of created folders.")]
            public List<string>? CreatedFolderGuids { get; set; }
            [Description("List of errors encountered during folder creation.")]
            public List<string>? Errors { get; set; }
        }
    }
}
