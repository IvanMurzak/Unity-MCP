#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static class ProcessUtils
    {
        public static async Task<(string output, string error)> Run(ProcessStartInfo processStartInfo, bool suppressError = false)
        {
            Debug.Log($"{Consts.Log.Tag} Command: <color=#8CFFD1>{processStartInfo.FileName} {processStartInfo.Arguments}</color>");

            var output = string.Empty;
            var error = string.Empty;

            // Ensure proper configuration for macOS and other platforms
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            FixEnvironmentPath(processStartInfo.EnvironmentVariables);

            await Task.Run(() =>
            {
                try
                {
                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.Start();

                        // Read the output and error streams
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    if (suppressError)
                    {
                        Debug.Log($"{Consts.Log.Tag} Failed to execute command: {processStartInfo.FileName} {processStartInfo.Arguments}");
                    }
                    else
                    {
                        Debug.LogError($"{Consts.Log.Tag} Failed to execute command: {processStartInfo.FileName} {processStartInfo.Arguments}");
                        Debug.LogException(ex);
                    }

                    error = ex.Message;
                    // error = "Failed to execute 'dotnet' command. Ensure 'dotnet' CLI is installed and accessible in the environment"
                }
            });
            return (output, error);
        }
        /// <summary>
        /// Fixes the environment path for the process to include dotnet paths.
        /// This is necessary for macOS and other platforms where the dotnet CLI might not be in the default PATH.
        /// Or may not be visible from the Unity environment. It includes paths for local device and for GitHub actions environment.
        /// </summary>
        static void FixEnvironmentPath(StringDictionary envVariables)
        {
            const string PATH = "PATH";
            var dotnetPaths = new string[]
            {
                Startup.ExpectedDotnetInstallDir,

                // #if UNITY_EDITOR_WIN
                // "C:/Program Files/dotnet", // windows (device)
                // #endif

                // #if UNITY_EDITOR_OSX
                // "~/.dotnet/tools", // macos (device)
                // "/usr/local/share/dotnet", // macos (device)
                // "/Users/runner/.dotnet", // macos (GitHub actions)
                // #endif

                // #if UNITY_EDITOR_LINUX
                // "$HOME/.dotnet", // linux (GitHub actions)
                // "/usr/share/dotnet", // ubuntu (GitHub actions)
                // #endif
            };
            foreach (var dotnetPath in dotnetPaths)
            {
                if (envVariables.ContainsKey(PATH) == false)
                {
                    envVariables[PATH] = dotnetPath;
                    continue;
                }
                var envPath = envVariables[PATH];
                if (envPath.Contains(dotnetPath) == false)
                {
                    envVariables[PATH] = string.IsNullOrEmpty(envPath)
                        ? dotnetPath
                        : $"{envPath}{System.IO.Path.PathSeparator}{dotnetPath}";
                }
            }
            foreach (var dotnetPath in dotnetPaths)
            {
                var envPath = Environment.GetEnvironmentVariable(PATH);
                if (envPath == null)
                {
                    Environment.SetEnvironmentVariable(PATH, dotnetPath);
                    continue;
                }
                if (envPath.Contains(dotnetPath) == false)
                {
                    Environment.SetEnvironmentVariable(PATH, string.IsNullOrEmpty(envPath)
                        ? dotnetPath
                        : $"{envPath}{System.IO.Path.PathSeparator}{dotnetPath}");
                }
            }
        }
    }
}