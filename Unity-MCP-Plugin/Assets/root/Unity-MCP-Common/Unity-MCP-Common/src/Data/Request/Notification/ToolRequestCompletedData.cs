#nullable enable

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ToolRequestCompletedData
    {
        public string RequestId { get; set; } = string.Empty;
        public ResponseCallTool Result { get; set; } = null!;

        public override string ToString()
            => $"RequestId: {RequestId}, Result: {Result}";
    }
}