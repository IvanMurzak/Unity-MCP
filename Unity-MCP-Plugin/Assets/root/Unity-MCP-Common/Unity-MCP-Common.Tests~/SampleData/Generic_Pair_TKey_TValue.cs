// Generic pair with constraints and methods
using System;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Pair<TKey, TValue>
        where TKey : notnull
    {
        public TKey Key { get; set; }
        public TValue? Value { get; set; }

        public Pair(TKey key, TValue? value = default)
        {
            Key = key;
            Value = value;
        }

        public (TKey, TValue?) ToTuple() => (Key, Value);

        public async Task<(TKey, TValue?)> ToTupleAsync()
        {
            await Task.Yield();
            return (Key, Value);
        }

        public bool TryGetValue(out TValue? value)
        {
            value = Value;
            return Value is not null;
        }
    }
}
