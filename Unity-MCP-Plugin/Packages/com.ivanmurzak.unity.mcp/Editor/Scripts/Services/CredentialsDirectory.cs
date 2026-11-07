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
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Explicit machine-credential store location for development/test editors, the twin of Unreal's
    /// <c>UNREAL_MCP_CREDENTIALS_DIR</c> and Godot's <c>GODOT_MCP_CREDENTIALS_DIR</c>. A path, never a
    /// token: the editor still reads (and refreshes) the machine credential store, only from this folder
    /// instead of the OS user profile. Needed where the profile .NET resolves is not the one the launching
    /// App signed in to: the AI Game Dev App's local dev stand points HOME/USERPROFILE at a dev profile,
    /// which <c>Environment.GetFolderPath(UserProfile)</c> ignores on Windows. Read from the editor's
    /// process environment only (the launcher sets it), never from a project file.
    /// </summary>
    internal static class CredentialsDirectory
    {
        public const string EnvName = "UNITY_MCP_CREDENTIALS_DIR";

        /// <summary>The folder from <see cref="EnvName"/>, or null for the platform default. Only absolute paths.</summary>
        public static string? Resolve(Func<string, string?>? getEnv = null)
        {
            var directory = (getEnv ?? Environment.GetEnvironmentVariable)(EnvName);
            if (string.IsNullOrWhiteSpace(directory))
                return null;
            if (!Path.IsPathRooted(directory) || IsDriveRelative(directory!))
                throw new ArgumentException($"{EnvName} must be an absolute path.");
            return Path.GetFullPath(directory);
        }

        /// <summary>
        /// <see cref="Resolve"/> that never throws: an invalid value is logged and ignored (the platform
        /// default is used), so a bad launcher value cannot break the editor's credential stack.
        /// </summary>
        public static string? ResolveOrDefault(ILogger? logger, Func<string, string?>? getEnv = null)
        {
            try
            {
                return Resolve(getEnv);
            }
            // ArgumentException: relative or invalid characters; NotSupportedException/IOException
            // (PathTooLongException): what Path.GetFullPath throws on .NET Framework/Mono for a bad path.
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is IOException)
            {
                logger?.LogError("{EnvName} is invalid ({Message}). Using the default machine credential store.", EnvName, e.Message);
                return null;
            }
        }

        // Path.IsPathFullyQualified is missing from Unity's .NET Framework 4.x API profile:
        // "C:folder" and "\folder" are rooted but still relative to a current drive/directory on Windows.
        static bool IsDriveRelative(string path)
        {
            if (Path.DirectorySeparatorChar != '\\')
                return false;
            if (path.Length >= 2 && path[1] == ':')
                return path.Length < 3 || (path[2] != '\\' && path[2] != '/');
            // "\\server\share" (UNC) is fully qualified; a single leading separator is not.
            return (path[0] == '\\' || path[0] == '/') && !(path.Length >= 2 && (path[1] == '\\' || path[1] == '/'));
        }
    }
}
