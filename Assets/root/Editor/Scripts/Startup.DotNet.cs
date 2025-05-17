#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    static partial class Startup
    {
        const string DefaultDotNetVersion = "9.0";
        public static string ExpectedDotnetInstallDir
#if UNITY_EDITOR_WIN
            => System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "dotnet"
            );
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            => System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dotnet"
            );
#endif

        public static async Task InstallDotNetIfNeeded(string version = DefaultDotNetVersion, bool force = false)
        {
            // Check if .NET SDK is installed
            if (force)
            {
                Debug.Log($"{Consts.Log.Tag} Force installing .NET SDK...");
            }
            else
            {
                var isDotnetInstalled = await IsDotNetInstalled();
                if (isDotnetInstalled)
                    return;
                Debug.Log($"{Consts.Log.Tag} .NET SDK is not installed. Installing...");
            }

            // Install .NET SDK if not installed
#if UNITY_EDITOR_WIN
            await InstallDotnet_Windows(version);
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            await InstallDotnet_Linux(version);
#else
            Debug.LogError($"{Consts.Log.Tag} Unsupported platform for .NET SDK installation.");
            return;
#endif
        }

        public static async Task<bool> IsDotNetInstalled()
        {
            var (output, error) = await ProcessUtils.Run(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version"
            }, suppressError: true);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log($"{Consts.Log.Tag} .NET SDK is not installed.");
                // UnityEngine.Debug.LogError($"{Consts.Log.Tag} Error checking .NET SDK version: {error}");
                return false;
            }

            if (string.IsNullOrEmpty(output))
            {
                Debug.Log($"{Consts.Log.Tag} .NET SDK is not installed.");
                // UnityEngine.Debug.LogError($"{Consts.Log.Tag} .NET SDK is not installed.");
                return false;
            }

            Debug.Log($"{Consts.Log.Tag} .NET SDK version: {output}");
            return output.StartsWith(DefaultDotNetVersion);
        }

        static async Task InstallDotnet_Windows(string version)
        {
            Debug.Log($"{Consts.Log.Tag} Downloading .NET SDK installer script...");

            var tempScript = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dotnet-install.ps1");
            var (downloadOutput, downloadError) = await ProcessUtils.Run(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '{tempScript}'\""
            });

            if (!string.IsNullOrEmpty(downloadError))
            {
                Debug.LogError($"{Consts.Log.Tag} Error downloading installer: {downloadError}");
                return;
            }

            // 1. Install .NET SDK with a specific install directory we control
            var dotnetInstallDir = ExpectedDotnetInstallDir;

            Debug.Log($"{Consts.Log.Tag} Installing .NET SDK version {version} to {dotnetInstallDir}...");
            var (output, error) = await ProcessUtils.Run(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-ExecutionPolicy Bypass -File \"{tempScript}\" -Channel {version} -InstallDir \"{dotnetInstallDir}\""
            });

            if (!string.IsNullOrEmpty(output))
                Debug.Log($"{Consts.Log.Tag} {output}");

            if (!string.IsNullOrEmpty(error))
                Debug.LogError(error);

            // 2. Update PATH in current process
            var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";
            if (!currentPath.Contains(dotnetInstallDir))
            {
                string newPath = dotnetInstallDir + ";" + currentPath;
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
                Debug.Log($"{Consts.Log.Tag} Updated current process PATH with .NET SDK location");
            }

            // 3. Also try to update user PATH so it persists
            try
            {
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                if (!userPath.Contains(dotnetInstallDir))
                {
                    string newUserPath = dotnetInstallDir + ";" + userPath;
                    Environment.SetEnvironmentVariable("PATH", newUserPath, EnvironmentVariableTarget.User);
                    Debug.Log($"{Consts.Log.Tag} Updated user PATH environment variable with .NET SDK location");
                }
            }
            catch (System.Security.SecurityException)
            {
                Debug.LogWarning($"{Consts.Log.Tag} Couldn't update user PATH environment variable (insufficient permissions)");
            }
        }
        static async Task InstallDotnet_Linux(string version)
        {
            var dotnetInstallDir = ExpectedDotnetInstallDir;
            Debug.Log($"{Consts.Log.Tag} Downloading .NET SDK installer script...");
            Debug.Log($"{Consts.Log.Tag} Installing .NET SDK version {version} to {dotnetInstallDir}...");

            var (output, error) = await ProcessUtils.Run(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel {version} --install-dir '{dotnetInstallDir}'\""
            });

            if (!string.IsNullOrEmpty(output))
                Debug.Log($"{Consts.Log.Tag} {output}");

            if (!string.IsNullOrEmpty(error))
                Debug.LogError(error);
        }
    }
}