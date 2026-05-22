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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateFolderExecutor : LazyNodeExecutor
    {
        protected readonly string[] _folders;
        protected readonly string _fullPath;

        int firstCreatedFolderIndex = -1;

        public string FolderPath => _fullPath;

        public CreateFolderExecutor(params string[] folders) : base()
        {
            folders = folders ?? throw new ArgumentNullException(nameof(folders));
            if (folders.Length == 0)
                throw new ArgumentException("At least one folder must be specified.", nameof(folders));

            if (folders[0] != "Assets")
                throw new ArgumentException("The first folder must be 'Assets'.", nameof(folders));

            _folders = folders;
            _fullPath = string.Join("/", folders);

            SetAction(() =>
            {
                for (int i = 0; i < _folders.Length; i++)
                {
                    var folderPath = string.Join("/", _folders.Take(i + 1));

                    if (AssetDatabase.IsValidFolder(folderPath))
                        continue;

                    Debug.Log($"Creating folder: {folderPath}");

                    AssetDatabase.CreateFolder(
                        parentFolder: _folders[i - 1],
                        newFolderName: _folders[i]);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    if (firstCreatedFolderIndex == -1)
                        firstCreatedFolderIndex = i;
                }

                // Unity's CreateFolder has some delay, so we need to make the folder on our own
                if (Directory.Exists(_fullPath))
                    return;

                Directory.CreateDirectory(_fullPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            });
        }

        protected override void PostExecute(object? input)
        {
            base.PostExecute(input);

            if (firstCreatedFolderIndex < 0)
                return;

            for (int i = _folders.Length - 1; i >= firstCreatedFolderIndex; i--)
            {
                var folderPath = string.Join("/", _folders.Take(i + 1));
                Debug.Log($"Deleting folder: {folderPath}");
                AssetDatabase.DeleteAsset(folderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
