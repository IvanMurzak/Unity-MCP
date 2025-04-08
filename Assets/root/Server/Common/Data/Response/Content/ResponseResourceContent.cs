#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.Unity.MCP.Common.Data
{
    public class ResponseResourceContent : IResponseResourceContent
    {
        public string uri { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? text { get; set; }
        public string? blob { get; set; }

        public ResponseResourceContent() { }
        public ResponseResourceContent(string uri, string? mimeType = null, string? text = null, string? blob = null)
        {
            this.uri = uri;
            this.mimeType = mimeType;
            this.text = text;
            this.blob = blob;
        }

        public static ResponseResourceContent CreateText(string uri, string text)
            => new ResponseResourceContent(uri, text: text);

        public static ResponseResourceContent CreateBlob(string uri, string blob)
            => new ResponseResourceContent(uri, blob: blob);
    }
}