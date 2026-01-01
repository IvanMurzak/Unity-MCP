/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Tristyn Mackay (https://github.com/InMetaTech-Tristyn)  │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

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
                    Debug.LogError($"Secret store command '{fileName}' failed to start.");
                    return null;
                }

                if (input != null)
                {
                    // Best-effort clearing; the caller string may still be alive in managed memory.
                    var inputBytes = Encoding.UTF8.GetBytes(input);
                    input = null;
                    process.StandardInput.BaseStream.Write(inputBytes, 0, inputBytes.Length);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    Array.Clear(inputBytes, 0, inputBytes.Length);
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Task.WhenAll(outputTask, errorTask).GetAwaiter().GetResult();
                var output = outputTask.Result;
                var error = errorTask.Result;

                if (process.ExitCode != 0)
                {
                    if (!ignoreNotFound)
                    {
                        var message = string.IsNullOrWhiteSpace(error)
                            ? $"Secret store command '{fileName}' failed with exit code {process.ExitCode}."
                            : $"Secret store command '{fileName}' failed with exit code {process.ExitCode}: {error.Trim()}";
                        Debug.LogError(message);
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
                    Debug.LogError($"Secret store command '{fileName}' not found. Install the required tool to enable secure storage.");
                }
                else
                {
                    Debug.LogError($"Secret store command '{fileName}' failed: {root.GetType().Name} {root.Message}");
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

        // Quote arguments for ProcessStartInfo.Arguments (not shell expansion).
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
