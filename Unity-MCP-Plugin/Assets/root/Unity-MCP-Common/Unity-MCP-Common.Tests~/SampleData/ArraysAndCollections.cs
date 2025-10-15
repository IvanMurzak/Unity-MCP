// Cases: Arrays and Collections for primitives and complex types
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class ArraysAndCollections
    {
        public int[] GetIntArray() => new[] { 1, 2, 3 };
        public string[][] GetJaggedStrings() => new[] { new[] { "a", "b" }, new[] { "c" } };
        public int[,] GetMatrix() => new int[2, 2] { { 1, 2 }, { 3, 4 } };

        public List<Person> GetPeopleList() => new List<Person> { new Person { Age = 30 }, new Person { Age = 40 } };
        public Dictionary<string, Address> GetAddressMap() => new Dictionary<string, Address>
        {
            { "home", new Address { Street = "1st St", City = "A", Country = "X" } },
            { "work", new Address { Street = "2nd St", City = "B", Country = "Y" } },
        };

        public (int, string) GetTuple() => (42, "answer");
    }
}
