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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            var foldersToDelete = new List<string>();
            for (int i = _folders.Length - 1; i >= firstCreatedFolderIndex; i--)
                foldersToDelete.Add(string.Join("/", _folders.Take(i + 1)));

            TryDeleteFolders(foldersToDelete, attempt: 0, notBefore: EditorApplication.timeSinceStartup);
        }

        void ScheduleFolderCleanup(List<string> foldersToDelete, int attempt, double notBefore)
        {
            EditorApplication.delayCall += () => TryDeleteFolders(foldersToDelete, attempt, notBefore);
        }

        void TryDeleteFolders(List<string> foldersToDelete, int attempt, double notBefore)
        {
            const int MaxCleanupAttempts = 180;

            if (EditorApplication.timeSinceStartup < notBefore)
            {
                if (attempt < MaxCleanupAttempts)
                    ScheduleFolderCleanup(foldersToDelete, attempt + 1, notBefore);
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (attempt < MaxCleanupAttempts)
                    ScheduleFolderCleanup(foldersToDelete, attempt + 1, EditorApplication.timeSinceStartup + 1.0d);
                return;
            }

            if (HasActiveGenerationUnderFolders(foldersToDelete))
            {
                if (attempt < MaxCleanupAttempts)
                    ScheduleFolderCleanup(foldersToDelete, attempt + 1, EditorApplication.timeSinceStartup + 1.0d);
                return;
            }

            var needsRetry = false;
            AssetDatabase.ReleaseCachedFileHandles();

            foreach (var folderPath in foldersToDelete)
            {
                if (!AssetDatabase.IsValidFolder(folderPath))
                    continue;

                Debug.Log($"Deleting folder: {folderPath}");
                if (!AssetDatabase.DeleteAsset(folderPath) && AssetDatabase.IsValidFolder(folderPath))
                    needsRetry = true;
            }

            if (needsRetry && attempt < MaxCleanupAttempts)
                ScheduleFolderCleanup(foldersToDelete, attempt + 1, EditorApplication.timeSinceStartup + 1.0d);
        }

        static bool HasActiveGenerationUnderFolders(List<string> foldersToDelete)
        {
            foreach (var folder in foldersToDelete)
            {
                if (HasActiveGenerationInFolder(folder))
                    return true;
            }

            return false;
        }

        static bool HasActiveGenerationInFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return false;

            var prefix = folderPath.EndsWith("/", StringComparison.Ordinal)
                ? folderPath
                : folderPath + "/";

            return HasActiveGeneration(
                       "com.IvanMurzak.Unity.MCP.Composer.ComposerGenerationHistory",
                       prefix)
                   || HasActiveGeneration(
                       "com.IvanMurzak.Unity.MCP.Texture.TextureGenerationHistory",
                       prefix);
        }

        static bool HasActiveGeneration(string historyTypeName, string assetPathPrefix)
        {
            var historyType = FindType(historyTypeName);
            if (historyType == null)
                return false;

            var entriesProperty = historyType.GetProperty(
                "Entries",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var entries = entriesProperty?.GetValue(null) as System.Collections.IEnumerable;
            if (entries == null)
                return false;

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                var entryType = entry.GetType();
                var assetPathField = entryType.GetField("assetPath");
                var assetPath = assetPathField?.GetValue(entry) as string;
                if (string.IsNullOrWhiteSpace(assetPath)
                    || !assetPath.StartsWith(assetPathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var stateField = entryType.GetField("state");
                var stateValue = stateField?.GetValue(entry);
                if (stateValue == null)
                    continue;

                if (string.Equals(stateValue.ToString(), "Generating", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static Type? FindType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null)
                    continue;

                type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

    }
}
