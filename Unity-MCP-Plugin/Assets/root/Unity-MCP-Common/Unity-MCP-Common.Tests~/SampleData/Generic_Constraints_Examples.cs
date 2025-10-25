// Cases: Generics with constraints and nested generics
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public interface IIdentifiable
    {
        Guid Id { get; }
    }

    public class Identifiable : IIdentifiable
    {
        public Guid Id { get; init; } = Guid.NewGuid();
    }

    public class Factory<T> where T : new()
    {
        public T Create() => new T();
    }

    public class Registry<TKey, TValue>
        where TKey : notnull
        where TValue : IIdentifiable
    {
        private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        public void Add(TKey key, TValue value) => _map[key] = value;
        public bool TryGet(TKey key, out TValue value) => _map.TryGetValue(key, out value!);
    }

    public class NestedGeneric<T>
    {
        public Container<List<T>> Wrapper { get; } = new Container<List<T>>();
    }
}
