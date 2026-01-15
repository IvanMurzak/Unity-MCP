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
                    var guid = AssetDatabase.CreateFolder(input.parentFolderPath, input.newFolderName);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        response.createdFolderGuids ??= new();
                        response.createdFolderGuids.Add(guid);
                    }
                    else
                    {
                        response.errors ??= new();
                        response.errors.Add($"Failed to create folder '{input.newFolderName}' in '{input.parentFolderPath}'.");
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

        public class CreateFolderInput
        {
            public string parentFolderPath = string.Empty;
            public string newFolderName = string.Empty;
        }
        public class CreateFolderResponse
        {
            public List<string>? createdFolderGuids;
            public List<string>? errors;
        }
    }
}
