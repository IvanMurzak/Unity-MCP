// Cases: value tuples and nullable reference types
using System;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class TupleAndNullable
    {
        public (int Sum, int Product) Calc(int a, int b) => (a + b, a * b);
        public string? MaybeString(bool flag) => flag ? "hi" : null;
        public Person? MaybePerson(bool flag) => flag ? new Person { Age = 1 } : null;
    }
}
