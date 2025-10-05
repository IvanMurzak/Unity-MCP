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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Build preprocessor that extracts and merges link.xml files from all packages.
    /// This ensures proper code stripping preservation during IL2CPP builds.
    /// </summary>
    public class LinkExtractor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string FileName = "link.xml";
        const string MergedFolderName = "packages-merged-link";

        string MergedFolder => Path.Combine(Application.dataPath, MergedFolderName);
        string MergedLinkFilePath => Path.Combine(MergedFolder, FileName);

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CreateMergedLinkFromPackages();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanupTemporaryFiles();
        }

        void CreateMergedLinkFromPackages()
        {
            var request = Client.List();

            // Wait for package manager request to complete
            while (!request.IsCompleted) { }

            if (request.Status == StatusCode.Success)
            {
                var linkXmlFiles = CollectLinkXmlFiles(request.Result);
                if (linkXmlFiles.Count == 0)
                    return;

                MergeAndSaveLinkFiles(linkXmlFiles);
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.LogError($"[LinkExtractor] Failed to list packages: {request.Error.message}");
            }
        }

        List<string> CollectLinkXmlFiles(PackageCollection packages)
        {
            var xmlPaths = new List<string>();

            foreach (var package in packages)
            {
                if (string.IsNullOrEmpty(package.resolvedPath))
                    continue;

                var packageLinkFiles = Directory.EnumerateFiles(
                    package.resolvedPath,
                    FileName,
                    SearchOption.AllDirectories
                );

                xmlPaths.AddRange(packageLinkFiles);
            }

            return xmlPaths;
        }

        void MergeAndSaveLinkFiles(List<string> xmlPaths)
        {
            var xmlDocuments = xmlPaths.Select(XDocument.Load).ToArray();
            if (xmlDocuments.Length == 0)
                return;

            var mergedXml = xmlDocuments[0];

            for (int i = 1; i < xmlDocuments.Length; i++)
            {
                var elements = xmlDocuments[i].Root?.Elements();
                if (elements != null)
                {
                    mergedXml.Root?.Add(elements);
                }
            }

            if (!Directory.Exists(MergedFolder))
                Directory.CreateDirectory(MergedFolder);

            mergedXml.Save(MergedLinkFilePath);

            Debug.Log($"[LinkExtractor] Merged {xmlDocuments.Length} link.xml files to {MergedLinkFilePath}");
        }

        void CleanupTemporaryFiles()
        {
            if (Directory.Exists(MergedFolder))
                Directory.Delete(MergedFolder, recursive: true);

            var metaFilePath = MergedFolder + ".meta";
            if (File.Exists(metaFilePath))
                File.Delete(metaFilePath);

            UnityEditor.AssetDatabase.Refresh();
        }
    }
}