#if !UNITY_5_3_OR_NEWER
using R3;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class EventAppToolsChange : Subject<EventAppToolsChange.EventData>
    {
        public class EventData
        {
            public string ConnectionId { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
        }
    }
}
#endif