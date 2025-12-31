#if UNITY_EDITOR_LINUX

#nullable enable

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
    }
}

#endif
