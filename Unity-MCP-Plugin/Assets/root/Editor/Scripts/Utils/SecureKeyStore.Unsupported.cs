#if !UNITY_EDITOR_WIN && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX

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
            return null;
        }

        private static void Write(string targetName, string value)
        {
        }

        private static void DeleteInternal(string targetName)
        {
        }
    }
}

#endif
