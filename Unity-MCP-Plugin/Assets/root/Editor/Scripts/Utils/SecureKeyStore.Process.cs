#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX

#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static partial class SecureKeyStore
    {
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
                {
                    Debug.LogWarning($"[Warning] Secret store command '{fileName}' failed to start.");
                    return null;
                }

                if (input != null)
                {
                    var inputBytes = Encoding.UTF8.GetBytes(input);
                    process.StandardInput.BaseStream.Write(inputBytes, 0, inputBytes.Length);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    Array.Clear(inputBytes, 0, inputBytes.Length);
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Task.WaitAll(outputTask, errorTask);
                var output = outputTask.Result;
                var error = errorTask.Result;

                if (process.ExitCode != 0)
                {
                    if (!ignoreNotFound)
                    {
                        var message = string.IsNullOrWhiteSpace(error)
                            ? $"[Warning] Secret store command '{fileName}' failed with exit code {process.ExitCode}."
                            : $"[Warning] Secret store command '{fileName}' failed with exit code {process.ExitCode}: {error.Trim()}";
                        Debug.LogWarning(message);
                    }

                    return null;
                }

                return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                if (root is Win32Exception win32Ex && win32Ex.NativeErrorCode == 2)
                {
                    Debug.LogWarning($"[Warning] Secret store command '{fileName}' not found. Install the required tool to enable secure storage.");
                }
                else
                {
                    Debug.LogWarning($"[Warning] Secret store command '{fileName}' failed: {root.GetType().Name} {root.Message}");
                }

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
