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

            // Create merged document with duplicate detection
            var mergedRoot = new XElement("linker");
            var seenAssemblies = new HashSet<string>();

            foreach (var doc in xmlDocuments)
            {
                var elements = doc.Root?.Elements();
                if (elements == null)
                    continue;

                foreach (var element in elements)
                {
                    // Create a unique key for this element
                    var elementKey = CreateElementKey(element);

                    if (seenAssemblies.Add(elementKey))
                    {
                        mergedRoot.Add(new XElement(element));
                    }
                }
            }

            var mergedXml = new XDocument(mergedRoot);

            if (!Directory.Exists(MergedFolder))
                Directory.CreateDirectory(MergedFolder);

            mergedXml.Save(MergedLinkFilePath);

            Debug.Log($"[LinkExtractor] Merged {xmlDocuments.Count} link.xml files ({seenAssemblies.Count} unique elements) to {MergedLinkFilePath}");
        }

        string CreateElementKey(XElement element)
        {
            // For assembly elements, use fullname attribute
            if (element.Name.LocalName == "assembly")
            {
                var fullname = element.Attribute("fullname")?.Value;
                return fullname != null ? $"assembly:{fullname}" : element.ToString();
            }

            // For other elements (type, method, etc.), use the full element representation
            return element.ToString();
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