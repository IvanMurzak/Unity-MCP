/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System.Linq;
using System.Xml.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class LinkXmlMergerTests
    {
        static XDocument Merge(params string[] fragments)
            => LinkXmlMerger.Merge(fragments.Select(xml => XDocument.Parse("<linker>" + xml + "</linker>")));

        [Fact]
        public void DifferentTypesInTheSameAssemblyAreRetained()
        {
            var result = Merge(
                "<assembly fullname='Game'><type fullname='Game.Inventory' preserve='all'/></assembly>",
                "<assembly fullname='Game'><type fullname='Game.SaveData' preserve='all'/></assembly>");

            Assert.Equal(new[] { "Game.Inventory", "Game.SaveData" },
                result.Descendants("type").Select(type => (string?)type.Attribute("fullname")).OrderBy(name => name));
        }

        [Fact]
        public void DifferentMembersOfTheSameTypeAreRetained()
        {
            var result = Merge(
                "<assembly fullname='Game'><type fullname='Game.State'><method name='Save'/></type></assembly>",
                "<assembly fullname='Game'><type fullname='Game.State'><method name='Load'/></type></assembly>");

            Assert.Equal(new[] { "Load", "Save" },
                result.Descendants("method").Select(method => (string?)method.Attribute("name")).OrderBy(name => name));
        }

        [Theory]
        [InlineData("<assembly fullname='Game' preserve='all'/>")]
        [InlineData("<assembly fullname='Game'/>")]
        [InlineData("<assembly fullname='Game' ignoreIfMissing='1'><type fullname='Game.State' preserve='all'/></assembly>")]
        [InlineData("<assembly fullname='Game'><type fullname='Game.State' preserve='methods'/></assembly>")]
        public void DistinctDirectivesSurviveInEitherOrder(string other)
        {
            const string first = "<assembly fullname='Game'><type fullname='Game.State' preserve='fields'/></assembly>";
            var expected = new[] { XElement.Parse(first).ToString(), XElement.Parse(other).ToString() }.OrderBy(xml => xml);

            foreach (var inputs in new[] { new[] { first, other }, new[] { other, first } })
            {
                var result = Merge(inputs);
                Assert.Equal(expected, result.Root!.Elements().Select(element => element.ToString()).OrderBy(xml => xml));
            }
        }

        [Fact]
        public void IdenticalDirectivesAreNotRepeated()
        {
            const string assembly = "<assembly fullname='Game'><type fullname='Game.State' preserve='all'/></assembly>";
            Assert.Single(Merge(assembly, assembly).Root!.Elements());
        }

        [Fact]
        public void InputsAreNotReparentedOrModified()
        {
            var source = XDocument.Parse("<linker><assembly fullname='Game' preserve='all'/></linker>");
            var before = source.ToString();
            var result = LinkXmlMerger.Merge(new[] { source });
            result.Root!.Element("assembly")!.SetAttributeValue("preserve", "nothing");

            Assert.Equal(before, source.ToString());
        }
    }
}
