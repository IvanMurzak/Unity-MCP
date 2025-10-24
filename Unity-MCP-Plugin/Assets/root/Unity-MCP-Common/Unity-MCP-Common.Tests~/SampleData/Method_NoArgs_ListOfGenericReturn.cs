// Method with no arguments returning List<T>
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Method_NoArgs_ListOfGenericReturn<T>
    {
        public List<T> Do()
        {
            return new List<T>();
        }
    }
}
