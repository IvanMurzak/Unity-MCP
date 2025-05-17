#if !UNITY_5_3_OR_NEWER
using System;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Data;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public interface IRemoteApp : IToolResponseReceiver, IResourceResponseReceiver, IDisposable
    {
        Task<IResponseData<string>> OnListToolsUpdated(string data);
        Task<IResponseData<string>> OnListResourcesUpdated(string data);
    }

    public interface IToolResponseReceiver
    {
        // Task RespondOnCallTool(IResponseData<IResponseCallTool> data, CancellationToken cancellationToken = default);
        // Task RespondOnListTool(IResponseData<List<IResponseListTool>> data, CancellationToken cancellationToken = default);
    }

    public interface IResourceResponseReceiver
    {
        // Task RespondOnResourceContent(IResponseData<List<IResponseResourceContent>> data, CancellationToken cancellationToken = default);
        // Task RespondOnListResources(IResponseData<List<IResponseListResource>> data, CancellationToken cancellationToken = default);
        // Task RespondOnListResourceTemplates(IResponseData<List<IResponseResourceTemplate>> data, CancellationToken cancellationToken = default);
    }
}
#endif