#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.UnityMCP.Common.Data
{
    public interface ICommandData : IDisposable
    {
        string? Class { get; set; }
        string? Method { get; set; }
        IDictionary<string, object?>? Parameters { get; set; }
    }
}