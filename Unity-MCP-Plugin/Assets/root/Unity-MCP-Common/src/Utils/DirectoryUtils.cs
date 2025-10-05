/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP
{
    public static class DirectoryUtils
    {
        public static void Delete(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }
        public static void Copy(string sourceDir, string destinationDir, params string[] ignorePatterns)
        {
            // Ensure the destination directory exists
            Directory.CreateDirectory(destinationDir);

            // Compile ignore patterns into regex
            var ignoreRegexes = ignorePatterns.Select(pattern =>
            new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase)).ToArray();

            // Helper function to check if a path matches any ignore pattern
            bool IsIgnored(string path) => ignoreRegexes.Any(regex => regex.IsMatch(path));

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (IsIgnored(file))
                {
                    // UnityEngine.Debug.LogWarning($"Ignored file: {file}");
                    continue;
                }

                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                // UnityEngine.Debug.Log($"Copying file: {file}\n{destFile}");
                File.Copy(file, destFile, overwrite: true);
            }

            // Copy all subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                if (IsIgnored(subDir))
                {
                    // UnityEngine.Debug.LogWarning($"Ignored dir: {subDir}");
                    continue;
                }

                var destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                Copy(subDir, destSubDir);
            }
        }
    }
}
