// Generic container covering various shapes for T
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Container<T>
    {
        public T? Value { get; set; }
        public List<T> Items { get; } = new List<T>();
        public Dictionary<string, T?> Map { get; } = new Dictionary<string, T?>();
        public T[] Array { get; set; } = System.Array.Empty<T>();

        public (int Count, T? First) Summary()
        {
            return (Items.Count, Items.Count > 0 ? Items[0] : default);
        }

        public void Add(T item) => Items.Add(item);
        public void Put(string key, T? value) => Map[key] = value;
    }
}
