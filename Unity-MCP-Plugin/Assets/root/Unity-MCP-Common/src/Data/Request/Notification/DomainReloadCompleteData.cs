#nullable enable

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class DomainReloadCompletedData
    {
        public string[]? PendingRequestIds { get; set; }
        public string? ConnectionId { get; set; }
    }
}