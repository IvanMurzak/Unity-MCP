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
#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;
#endif

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Build preprocessor that extracts and merges link.xml files from all packages.
    /// This ensures proper code stripping preservation during IL2CPP builds.
    /// </summary>
    public class LinkExtractor
#if UNITY_EDITOR
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#endif
    {
#if UNITY_EDITOR
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
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(10); // Prevent busy-wait CPU spinning
            }

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
            var xmlDocuments = new List<XDocument>();

            // Load XML files with error handling
            foreach (var path in xmlPaths)
            {
                try
                {
                    var doc = XDocument.Load(path);
                    if (doc.Root?.Name.LocalName == "linker")
                    {
                        xmlDocuments.Add(doc);
                    }
                    else
                    {
                        Debug.LogWarning($"[LinkExtractor] Skipping invalid link.xml (no 'linker' root): {path}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LinkExtractor] Failed to load {path}: {ex.Message}");
                }
            }

            if (xmlDocuments.Count == 0)
                return;

            var mergedXml = MergeDocuments(xmlDocuments);

            if (!Directory.Exists(MergedFolder))
                Directory.CreateDirectory(MergedFolder);

            mergedXml.Save(MergedLinkFilePath);

            Debug.Log($"[LinkExtractor] Merged {xmlDocuments.Count} link.xml files ({mergedXml.Root!.Elements().Count()} unique elements) to {MergedLinkFilePath}");
        }
#endif

        internal static XDocument MergeDocuments(IEnumerable<XDocument> xmlDocuments)
        {
            var mergedRoot = new XElement("linker");
            var seenElements = new HashSet<string>();

            foreach (var doc in xmlDocuments)
            {
                var elements = doc.Root?.Elements();
                if (elements == null)
                    continue;

                foreach (var element in elements)
                {
                    // An assembly can have different member rules or conditions in each package.
                    // Keep those declarations separate so the linker applies every preservation.
                    if (seenElements.Add(element.ToString(SaveOptions.DisableFormatting)))
                    {
                        mergedRoot.Add(new XElement(element));
                    }
                }
            }

            return new XDocument(mergedRoot);
        }

#if UNITY_EDITOR
        void CleanupTemporaryFiles()
        {
            if (Directory.Exists(MergedFolder))
                Directory.Delete(MergedFolder, recursive: true);

            var metaFilePath = MergedFolder + ".meta";
            if (File.Exists(metaFilePath))
                File.Delete(metaFilePath);

            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }
}
