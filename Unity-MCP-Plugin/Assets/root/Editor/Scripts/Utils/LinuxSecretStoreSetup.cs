#if UNITY_EDITOR_LINUX

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Extensions.Unity.PlayerPrefsEx;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static class LinuxSecretStoreSetup
    {
        private const string SecretToolName = "secret-tool";
        private const string DbusLaunchName = "dbus-launch";
        private static PlayerPrefsBool DoNotAskAgain = new("Unity-MCP.LinuxSecretStore.DoNotAskAgain");
        private static bool promptShownThisSession;

        public static void Init()
        {
            EditorApplication.delayCall += CheckOnStartup;
        }

        public static void CheckFromMenu()
        {
            CheckAndPrompt(ignoreDoNotAskAgain: true, showSuccess: true);
        }

        private static void CheckOnStartup()
        {
            EditorApplication.delayCall -= CheckOnStartup;

            if (EnvironmentUtils.IsCi())
                return;

            CheckAndPrompt(ignoreDoNotAskAgain: false, showSuccess: false);
        }

        private static void CheckAndPrompt(bool ignoreDoNotAskAgain, bool showSuccess)
        {
            if (promptShownThisSession && !ignoreDoNotAskAgain)
                return;

            if (!ignoreDoNotAskAgain && DoNotAskAgain.Value)
                return;

            promptShownThisSession = true;

            if (IsSecretStoreReady())
            {
                if (showSuccess)
                    EditorUtility.DisplayDialog("AI Game Developer", "Linux credential store is ready. 'secret-tool' and 'dbus-launch' are available.", "OK");
                return;
            }

            var message = "Linux credential storage requires 'secret-tool' (libsecret) and 'dbus-launch' (dbus-x11).\n\nInstall them now?";
            var choice = EditorUtility.DisplayDialogComplex(
                "AI Game Developer",
                message,
                "Install",
                "Not now",
                "Don't ask again");

            if (choice == 2)
            {
                DoNotAskAgain.Value = true;
                return;
            }

            if (choice != 0)
                return;

            _ = InstallPackagesAsync();
        }

        private static bool IsSecretStoreReady()
        {
            return IsSecretToolAvailable() && IsDbusLaunchAvailable();
        }

        private static bool IsSecretToolAvailable()
        {
            if (FindExecutableInPath(SecretToolName) != null)
                return true;

            var commonPaths = new[]
            {
                "/usr/bin/secret-tool",
                "/usr/local/bin/secret-tool",
                "/bin/secret-tool"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return true;
            }

            return false;
        }

        private static bool IsDbusLaunchAvailable()
        {
            if (FindExecutableInPath(DbusLaunchName) != null)
                return true;

            var commonPaths = new[]
            {
                "/usr/bin/dbus-launch",
                "/usr/local/bin/dbus-launch",
                "/bin/dbus-launch"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return true;
            }

            return false;
        }

        private static async Task InstallPackagesAsync()
        {
            if (!TryBuildInstallCommand(out var installCommand, out var manualCommand))
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(
                        "AI Game Developer",
                        "No supported package manager was detected. Please install 'secret-tool' and 'dbus-launch' manually.",
                        "OK");
                    Debug.LogWarning("Install 'secret-tool' and 'dbus-launch' manually. Example command:\n" + manualCommand);
                };
                return;
            }

            var exitCode = await RunShellCommandAsync(installCommand);
            var installed = IsSecretStoreReady();

            EditorApplication.delayCall += () =>
            {
                if (exitCode == 0 && installed)
                {
                    EditorUtility.DisplayDialog(
                        "AI Game Developer",
                        "'secret-tool' and 'dbus-launch' were installed successfully.",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "AI Game Developer",
                        "Install did not complete successfully. Please install 'secret-tool' and 'dbus-launch' manually.",
                        "OK");
                    Debug.LogWarning("Manual install command:\n" + manualCommand);
                }
            };
        }

        private static bool TryBuildInstallCommand(out string installCommand, out string manualCommand)
        {
            installCommand = string.Empty;
            manualCommand = "Install 'secret-tool' (libsecret) and 'dbus-launch' (dbus-x11) using your distro package manager.";

            var baseCommand = BuildBaseInstallCommand();
            if (string.IsNullOrWhiteSpace(baseCommand))
                return false;

            manualCommand = "sudo " + baseCommand;

            if (IsRootUser())
            {
                installCommand = baseCommand;
                return true;
            }

            if (FindExecutableInPath("pkexec") != null)
            {
                installCommand = $"pkexec bash -lc {QuoteForShell(baseCommand)}";
                return true;
            }

            if (FindExecutableInPath("sudo") != null)
            {
                installCommand = $"sudo bash -lc {QuoteForShell(baseCommand)}";
                return true;
            }

            return false;
        }

        private static string BuildBaseInstallCommand()
        {
            if (FindExecutableInPath("apt-get") != null)
                return "apt-get update && apt-get install -y libsecret-tools dbus-x11";

            if (FindExecutableInPath("dnf") != null)
                return "dnf install -y libsecret dbus-x11";

            if (FindExecutableInPath("yum") != null)
                return "yum install -y libsecret dbus-x11";

            if (FindExecutableInPath("pacman") != null)
                return "pacman -Sy --noconfirm libsecret dbus";

            if (FindExecutableInPath("zypper") != null)
                return "zypper install -y libsecret-tools dbus-1-x11";

            return string.Empty;
        }

        private static async Task<int> RunShellCommandAsync(string command)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-lc " + QuoteForShell(command),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return -1;

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var waitTask = Task.Run(() => process.WaitForExit());

                await Task.WhenAll(outputTask, errorTask, waitTask);

                var output = await outputTask;
                var error = await errorTask;

                if (!string.IsNullOrWhiteSpace(output))
                    Debug.Log(output.Trim());

                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogWarning(error.Trim());

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Warning] Linux package install failed: {ex.GetBaseException().Message}");
                return -1;
            }
        }

        private static bool IsRootUser()
        {
            return string.Equals(Environment.UserName, "root", StringComparison.OrdinalIgnoreCase);
        }

        private static string? FindExecutableInPath(string fileName)
        {
            if (Path.IsPathRooted(fileName) && File.Exists(fileName))
                return fileName;

            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            foreach (var segment in path.Split(':'))
            {
                if (string.IsNullOrWhiteSpace(segment))
                    continue;

                var candidate = Path.Combine(segment, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        // Bash-safe quoting for "bash -lc" command strings (not ProcessStartInfo arguments).
        private static string QuoteForShell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "''";

            return "'" + value.Replace("'", "'\\''") + "'";
        }
    }
}

#endif
