#if UNITY_EDITOR_LINUX

#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static partial class SecureKeyStore
    {
        private static string BuildTargetName(string key)
        {
            return key;
        }

        private static string? Read(string targetName)
        {
            return RunSecretCommand("secret-tool",
                new[] { "lookup", "service", ServiceName, "account", targetName },
                null,
                ignoreNotFound: true);
        }

        private static void Write(string targetName, string value)
        {
            RunSecretCommand("secret-tool",
                new[] { "store", "--label", $"{DisplayName} {targetName}", "service", ServiceName, "account", targetName },
                value,
                ignoreNotFound: false);
        }

        private static void DeleteInternal(string targetName)
        {
            RunSecretCommand("secret-tool",
                new[] { "clear", "service", ServiceName, "account", targetName },
                null,
                ignoreNotFound: true);
        }

        private static string? RunSecretCommand(
            string fileName,
            string[] args,
            string? input,
            bool ignoreNotFound)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = BuildArguments(args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = input != null,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return null;

                if (input != null)
                {
                    process.StandardInput.Write(input);
                    process.StandardInput.Close();
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    if (!ignoreNotFound && !string.IsNullOrWhiteSpace(error))
                        Debug.LogWarning($"[Warning] Secret store command failed: {error.Trim()}");
                    return null;
                }

                return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Warning] Secret store command failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        private static string BuildArguments(string[] args)
        {
            if (args.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                builder.Append(QuoteArgument(args[i]));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            if (arg.IndexOfAny(new[] { ' ', '"', '\'' }) >= 0)
                return "\"" + arg.Replace("\"", "\\\"") + "\"";

            return arg;
        }
    }
}

#endif
