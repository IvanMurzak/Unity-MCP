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
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Screenshot
    {
        private const string SavePathDescription =
            "Optional file to also write the PNG to, for when the user wants the picture as a file: the image result itself " +
            "exists only in this conversation. Relative to the project root (or absolute under it): 'Screenshots/shot.png' stays " +
            "a plain file; a path under 'Assets/' becomes a Unity asset, imported at once with a .meta file. Missing folders are " +
            "created and .png is added when there is no extension. Refused: anything outside the project, Unity's own folders " +
            "(Library, Temp, Logs, obj, Packages, ProjectSettings, UserSettings), a name other than .png, an existing file " +
            "(it never overwrites). Without it nothing is written.";

        private const string SavePathNote =
            " To hand the user a file of the picture, pass 'savePath': the PNG is also written inside the project and the " +
            "result text says 'Saved to <absolute path>'.";

        private const string SavePathSkillSection =
            "\n\n## Saving a file\n\n" +
            "`savePath` (optional) also writes the PNG inside the project; the image is still returned and the text says " +
            "`Saved to <absolute path>`. Use it when the user wants the picture as a file. `Screenshots/shot.png` stays a plain " +
            "file; a path under `Assets/` is imported as a Unity asset with a `.meta`. Refused: outside the project, Unity's own " +
            "folders, a name other than `.png`, an existing file.";

        private const string PngExtension = ".png";

        // Unity owns these: Library and Temp are wiped without notice, so a "saved" file would just vanish later, and the others
        // hold project configuration. Refused so that an agent gets a clear answer instead of a file nobody can rely on.
        private static readonly string[] UnityOwnedFolders = { "Library", "Temp", "Logs", "obj", "Packages", "ProjectSettings", "UserSettings" };

        // Names Windows reserves for devices, extension or not: on older versions NUL.png writes nowhere and raises no error.
        private static readonly Regex WindowsDeviceName = new Regex(@"^(CON|PRN|AUX|NUL|COM\d|LPT\d)(\..*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        /// <summary>
        /// Checks an optional <c>savePath</c> against <paramref name="projectRoot"/>. Returns the refusal, or null when it is fine: then
        /// <paramref name="fullPath"/> is the absolute file to write, or null when no save was asked for. Writes nothing.
        /// Judged on the request as given, never on what a path API collapses it to: "Screenshots/." must not become "Screenshots.png".
        /// </summary>
        internal static string? CheckSavePath(string? savePath, string projectRoot, out string? fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(savePath))
                return null;

            var root = projectRoot.Replace('\\', '/').TrimEnd('/');
            string Refuse(string why) => $"Cannot save to '{savePath}': {why}";
            string RefuseOutside() => Refuse($"it must stay inside the project folder {root}. Pass a path relative to it, such as Screenshots/shot.png.");
            const string fileNameExample = "add a file name, such as Screenshots/shot.png.";

            var path = savePath!.Trim().Replace('\\', '/');
            if (path.Equals(root, PathComparison) || path.StartsWith(root + "/", PathComparison))
                path = path.Substring(root.Length);
            else if (path.StartsWith("/") || path.StartsWith("~") || path.Contains(":")) // rooted, UNC, home, drive, NTFS stream
                return RefuseOutside();

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (path.EndsWith("/") || parts.Length == 0 || parts[parts.Length - 1] is "." or "..")
                return Refuse($"it names a folder; {fileNameExample}");

            var kept = new List<string>();
            foreach (var part in parts)
            {
                if (part == ".")
                    continue;
                if (part == "..")
                {
                    if (kept.Count == 0)
                        return RefuseOutside();
                    kept.RemoveAt(kept.Count - 1);
                    continue;
                }
                // Windows drops a trailing dot or space when it opens a path, so ".. /x" climbs out although it reads as a name here.
                if (part.EndsWith(".") || part.EndsWith(" ") || part.IndexOfAny(new[] { '<', '>', '"', '|', '?', '*', ':' }) >= 0 || part.Any(char.IsControl) || WindowsDeviceName.IsMatch(part))
                    return Refuse($"'{part}' is not a usable name (it ends in a dot or a space, has a character Windows refuses, or is a device name). Pick another one.");
                kept.Add(part);
            }

            var name = kept[kept.Count - 1];
            if (Path.GetFileNameWithoutExtension(name).Trim('.').Length == 0)
                return Refuse($"it has no file name, only an extension; {fileNameExample}");

            var extension = Path.GetExtension(name);
            if (extension.Length > 0 && !extension.Equals(PngExtension, StringComparison.OrdinalIgnoreCase))
                return Refuse("the capture is a PNG, so the file name must end in .png.");

            if (kept.Count > 1 && UnityOwnedFolders.Contains(kept[0], StringComparer.OrdinalIgnoreCase))
                return Refuse($"{kept[0]}/ belongs to Unity, which may delete or rewrite what is in it. Use another folder, such as Screenshots/shot.png.");

            var full = root;
            foreach (var part in kept)
            {
                full += "/" + SpelledAsOnDisk(full, part);
                if (IsLink(full))
                    return Refuse($"{full} is a link (a symlink or a junction), which may lead out of the project. Use a real folder inside it.");
            }

            if (Directory.Exists(full))
                return Refuse($"it names a folder; {fileNameExample}");
            if (extension.Length == 0)
                full += PngExtension;
            if (IsLink(full) || File.Exists(full))
                return Refuse("the file already exists and a capture never overwrites one; pick another name.");

            fullPath = full;
            return null;
        }

        // Where the file system ignores case, "assets/x.png" lands in Assets/, but only the real spelling tells that it is an asset.
        private static string SpelledAsOnDisk(string folder, string name)
        {
            if (!Directory.Exists(folder))
                return name;
            var matches = Directory.GetFileSystemEntries(folder, name);
            return matches.Length == 1 ? Path.GetFileName(matches[0]) : name;
        }

        // A link's target cannot be read on every scripting backend Unity has, so any link below the project root counts as leading out.
        private static bool IsLink(string path)
        {
            try
            {
                return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return false;
            }
        }

        // The refusal goes out before any graphics check: a bad path is the caller's to fix in every editor, -nographics included.
        private static ResponseCallTool? SavePathError(string? savePath, out string? fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(savePath))
                return null;
            var refusal = CheckSavePath(savePath, MainThread.Instance.Run(() => UnityMcpPluginEditor.ProjectRootPath), out fullPath);
            return refusal == null ? null : ResponseCallTool.Error(refusal);
        }

        /// <summary>The tool's image result; with <paramref name="saveTo"/> the PNG is written first and the text says where. Main thread.</summary>
        internal static ResponseCallTool CaptureResult(byte[] png, string caption, string? saveTo)
        {
            if (saveTo == null)
                return ResponseCallTool.Image(png, McpPlugin.Common.Consts.MimeType.ImagePng, caption);

            var writeError = WriteCapture(saveTo, png);
            if (writeError != null)
                return ResponseCallTool.Error(writeError);

            return ResponseCallTool.Image(png, McpPlugin.Common.Consts.MimeType.ImagePng, $"{caption}\nSaved to {saveTo}{ImportIfAsset(saveTo)}");
        }

        private static string? WriteCapture(string fullPath, byte[] png)
        {
            const string nothingSaved = "nothing was saved.";
            FileStream stream;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                // CreateNew and not a check before the write: the file can appear while the capture renders.
                stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return File.Exists(fullPath)
                    ? $"{fullPath} already exists and a capture never overwrites one; {nothingSaved}"
                    : $"Could not write the capture to {fullPath} ({ex.Message}); {nothingSaved}";
            }

            try
            {
                using (stream)
                    stream.Write(png, 0, png.Length);
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                File.Delete(fullPath); // this call created it, and a partial PNG is worse than none
                return $"Could not write the capture to {fullPath} ({ex.Message}); {nothingSaved}";
            }
        }

        // Imports just this file: AssetDatabase.Refresh would also pick up unrelated pending changes, which can start a compile and a
        // domain reload in the middle of a screenshot call. Without an import the agent could not address the file as an asset yet.
        private static string ImportIfAsset(string fullPath)
        {
            var root = UnityMcpPluginEditor.ProjectRootPath.Replace('\\', '/').TrimEnd('/');
            var assetPath = fullPath.Substring(root.Length + 1);
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return string.Empty;

            AssetDatabase.ImportAsset(assetPath);
            return File.Exists(fullPath + ".meta")
                ? $"\nImported as the Unity asset {assetPath}, with a .meta file next to it."
                : "\nUnity did not import it (it ignores names that start with a dot or end in ~), so it stays a plain file.";
        }
    }
}
