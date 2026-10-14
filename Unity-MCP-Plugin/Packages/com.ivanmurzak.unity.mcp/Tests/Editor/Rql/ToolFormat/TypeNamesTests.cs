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
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ToolFormat
{
    public class TypeNamesTests
    {
        [Test]
        public void UniqueTypesGetTheirShortName()
        {
            var names = TypeNames.Assign(new[] { "com.IvanMurzak.ReflectorNet.Model.SerializedMember", "AIGD.GameObjectRef", "com.X.Tool_Screenshot-CameraView" });
            Assert.AreEqual("SerializedMember", names["com.IvanMurzak.ReflectorNet.Model.SerializedMember"]);
            Assert.AreEqual("GameObjectRef", names["AIGD.GameObjectRef"]);
            Assert.AreEqual("CameraView", names["com.X.Tool_Screenshot-CameraView"]);
        }

        [Test]
        public void ClashingShortNamesKeepJustEnoughNamespaceOnEveryType()
        {
            var a = new[] { "Game.Alpha.Settings", "Game.Beta.Settings", "Game.Item" };
            var forward = TypeNames.Assign(a);
            var backward = TypeNames.Assign(new[] { a[2], a[1], a[0] });
            Assert.AreEqual("Alpha.Settings", forward["Game.Alpha.Settings"]);
            Assert.AreEqual("Beta.Settings", forward["Game.Beta.Settings"]);
            Assert.AreEqual("Item", forward["Game.Item"]);
            CollectionAssert.AreEquivalent(forward, backward, "names must not depend on the order types are met");
        }

        [Test]
        public void ClashesNeedingDeeperNamespacesResolve()
        {
            var names = TypeNames.Assign(new[] { "A.Core.Settings", "B.Core.Settings" });
            Assert.AreEqual("A.Core.Settings", names["A.Core.Settings"]);
            Assert.AreEqual("B.Core.Settings", names["B.Core.Settings"]);
        }

        [Test]
        public void NestedAndIdenticalNamesStayDistinct()
        {
            var names = TypeNames.Assign(new[] { "Ns.Outer+Settings", "Ns.Settings" });
            Assert.AreNotEqual(names["Ns.Outer+Settings"], names["Ns.Settings"]);
            Assert.AreEqual("Outer.Settings", names["Ns.Outer+Settings"]);
        }

        [TestCase("Game.Wrapper(Game.Item)", "Wrapper<Item>")]
        [TestCase("System.Collections.Generic.Dictionary(System.String,Game.Item)", "Dictionary<String, Item>")]
        [TestCase("Game.Box(Game.Wrapper(Game.Item))", "Box<Wrapper<Item>>")]
        public void GenericNamesAreShortenedInside(string key, string expected) =>
            Assert.AreEqual(expected, TypeNames.Assign(new[] { key })[key]);

        [Test]
        public void ClrTypesUseTheReflectorNetForm()
        {
            Assert.AreEqual("System.Collections.Generic.List(System.Collections.Generic.List(System.String))", TypeNames.FullName(typeof(List<List<string>>)));
            Assert.AreEqual("List<List<String>>", TypeNames.Display(TypeNames.FullName(typeof(List<List<string>>)), 1));
        }

        [Test]
        public void TheTypeBlockListsClashingTypesUnderDistinctNames()
        {
            var schema = "{\"type\":\"object\",\"properties\":{" +
                "\"a\":{\"$ref\":\"#/$defs/Game.Alpha.Settings\"},\"b\":{\"$ref\":\"#/$defs/Game.Beta.Settings\"},\"w\":{\"$ref\":\"#/$defs/Game.Wrapper(Game.Item)\"}}," +
                "\"$defs\":{\"Game.Alpha.Settings\":{\"type\":\"object\",\"properties\":{\"speed\":{\"type\":\"number\"}}}," +
                "\"Game.Beta.Settings\":{\"type\":\"object\",\"properties\":{\"color\":{\"type\":\"string\"}}}," +
                "\"Game.Wrapper(Game.Item)\":{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"integer\"}}}}}";
            var text = RqlToolPresentation.SchemaText.Convert(schema);
            StringAssert.Contains("Alpha.Settings {speed float}", text.Types);
            StringAssert.Contains("Beta.Settings {color string}", text.Types);
            StringAssert.Contains("Wrapper<Item> {value int}", text.Types);
            Assert.AreEqual("Alpha.Settings", text.Literals["a"]);
            Assert.AreEqual("Beta.Settings", text.Literals["b"]);
            Assert.AreEqual("Wrapper<Item>", text.Literals["w"]);
        }
    }
}
