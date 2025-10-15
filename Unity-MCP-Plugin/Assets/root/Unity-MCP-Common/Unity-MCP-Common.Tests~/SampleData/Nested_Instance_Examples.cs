// Cases: properties/fields as instances of another class (including generics and self-reference)
namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    public class Holder
    {
        public Person? Owner { get; set; }
        public Container<Address> AddressBox { get; } = new Container<Address>();
        private Pair<string, Person>? _primary;

        public void SetPrimary(Pair<string, Person> p) => _primary = p;
        public Pair<string, Person>? GetPrimary() => _primary;
    }
}
