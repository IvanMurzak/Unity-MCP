#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.Unity.MCP.Common
{
  public static partial class Consts
  {
    public const string All = "*";
    public const string AllRecursive = "**";
    public const string PackageName = "com.ivanmurzak.unity.mcp";

    public static class Guid
    {
      public const string Zero = "00000000-0000-0000-0000-000000000000";
    }
    public static class MCP
    {
      public const int LinesLimit = 1000;
    }

    public static partial class Command
    {
      public static partial class ResponseCode
      {
        public const string Success = "[Success]";
        public const string Error = "[Error]";
        public const string Cancel = "[Cancel]";
      }
    }
    public static partial class MCP_Client
    {
      public static partial class ClaudeDesktop
      {
        public static string Config(string executablePath, string bodyName, int port, int? timeoutMs = null)
        {
          var timeoutMilliseconds = timeoutMs ?? Hub.DefaultTimeoutMs;
          
          return @"{
  ""{0}"": {
    ""Unity-MCP"": {
      ""command"": ""{1}"",
      ""args"": [
        ""--port={2}"",
        ""--timeout={3}""
      ]
    }
  }
}"
.Replace("{0}", bodyName)
.Replace("{1}", executablePath)
.Replace("{2}", port.ToString())
.Replace("{3}", timeoutMilliseconds.ToString());
        }

      }
    }
  }
}