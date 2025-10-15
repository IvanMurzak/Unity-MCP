// Complex class: Company with nested collections and references
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Company
    {
        public string Name { get; set; } = string.Empty;
        public Address? Headquarters { get; set; }
        public List<Person> Employees { get; } = new List<Person>();
        public Dictionary<string, List<Person>> Teams { get; } = new Dictionary<string, List<Person>>();
        public Dictionary<string, Dictionary<string, Person>> Directory { get; } = new();
    }
}
