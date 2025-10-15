// Complex class: Person with mixed fields/properties and nested types
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Person
    {
        // Fields
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        private DateTime _birthDate;

        // Properties
        public int Age { get; set; }
        public Address? Address { get; set; }
        public List<string> Tags { get; } = new List<string>();
        public Dictionary<string, int> Scores { get; } = new Dictionary<string, int>();

        // Arrays
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public string[][] JaggedAliases { get; set; } = Array.Empty<string[]>();
        public int[,] Matrix2x2 { get; set; } = new int[2, 2];

        // Methods using primitives and collections
        public void SetBirthDate(DateTime date) => _birthDate = date;
        public DateTime GetBirthDate() => _birthDate;

        public void AddTag(string tag) => Tags.Add(tag);
        public void SetScore(string key, int value) => Scores[key] = value;
    }
}
