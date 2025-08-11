using System;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public interface IRequestNotification : IRequestID, IDisposable
    {
        string? Path { get; set; }
        string? Name { get; set; }
        IDictionary<string, object?>? Parameters { get; set; }
    }
}