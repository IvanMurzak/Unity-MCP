using System;
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class DataArguments
    {
        public int Port { get; private set; }
        public int TimeoutMs { get; private set; }

        public DataArguments(string[] args)
        {
            Port = Consts.Hub.DefaultPort;
            TimeoutMs = Consts.Hub.DefaultTimeoutMs;

            ParseCommandLineArguments(args);
            ParseEnvironmentVariables();
        }

        private void ParseCommandLineArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                
                if (arg.StartsWith("--port="))
                {
                    if (int.TryParse(arg.Substring(7), out var parsedPort))
                        Port = parsedPort;
                }
                else if (arg.StartsWith("--timeout="))
                {
                    if (int.TryParse(arg.Substring(10), out var parsedTimeoutMs))
                        TimeoutMs = parsedTimeoutMs;
                }
                else if (i == 0 && int.TryParse(arg, out var posPort))
                {
                    Port = posPort;
                }
                else if (i == 1 && int.TryParse(arg, out var posTimeoutMs))
                {
                    TimeoutMs = posTimeoutMs;
                }
            }
        }

        private void ParseEnvironmentVariables()
        {
            var envPort = Environment.GetEnvironmentVariable(Consts.Env.Port);
            if (envPort != null && int.TryParse(envPort, out var parsedEnvPort))
                Port = parsedEnvPort;

            var envTimeout = Environment.GetEnvironmentVariable(Consts.Env.Timeout);
            if (envTimeout != null && int.TryParse(envTimeout, out var parsedEnvTimeoutMs))
                TimeoutMs = parsedEnvTimeoutMs;
        }
    }
}