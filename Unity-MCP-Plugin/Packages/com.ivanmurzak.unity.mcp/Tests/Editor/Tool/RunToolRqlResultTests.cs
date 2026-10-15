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
using System.Text.Json.Nodes;
using System.Threading;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// RunToolStructuredContentTests' return types under the RQL data layer: the value reaches the agent as RQL text (no
    /// structured content, no output schema) and reads back to the same data.
    /// </summary>
    public class RunToolRqlResultTests
    {
        private static (ResponseCallTool result, string text, JsonNode? data) Run(string method)
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var runTool = RunTool.CreateFromStaticMethod(UnityMcpPluginEditor.Instance.Reflector!, null, "rql-result", typeof(TestReturnTypeMethods).GetMethod(method)!);
            runTool.Binding = RqlToolBinding.Instance;
            var result = runTool.Run("request", CancellationToken.None).Result;
            Assert.AreEqual(ResponseStatus.Success, result.Status, result.GetMessage());
            Assert.IsNull(result.StructuredContent, "RQL results are text, not structured content");
            Assert.IsNull(((IRunTool)runTool).OutputSchema, "no output schema under RQL");
            return (result, string.Join("\n", result.Content.Select(c => c.Text)), ToolResults.Data(result, rql: true));
        }

        [TestCase(nameof(TestReturnTypeMethods.ReturnInt), "42")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnFloat), "3.14")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnBool), "true")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnString), "Hello World")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnEnum), "Success")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnMicrosoftLogLevel), "Information")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnVector3), "(1, 2, 3)")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnColor), "(1, 0, 0, 1)")]
        [TestCase(nameof(TestReturnTypeMethods.ReturnQuaternion), "(0, 0, 0, 1)")]
        public void ScalarsAndUnityStructsAreTheirRqlText(string method, string expected) =>
            Assert.AreEqual(expected, Run(method).text);

        [Test]
        public void NullIsSuccessWithoutAValue()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var runTool = RunTool.CreateFromStaticMethod(UnityMcpPluginEditor.Instance.Reflector!, null, "rql-result", typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnNull))!);
            runTool.Binding = RqlToolBinding.Instance;
            Assert.AreEqual(ResponseStatus.Success, runTool.Run("request", CancellationToken.None).Result.Status);
        }

        [Test]
        public void ACustomClassIsOneObject()
        {
            var data = Run(nameof(TestReturnTypeMethods.ReturnCustomClass)).data;
            Assert.AreEqual("John Doe", data!["Name"]!.GetValue<string>());
            Assert.AreEqual(30L, data["Age"]!.GetValue<long>());
        }

        [Test]
        public void ANestedClassIsWrittenOut()
        {
            var data = Run(nameof(TestReturnTypeMethods.ReturnNestedClass)).data;
            Assert.AreEqual("Acme Corp", data!["CompanyName"]!.GetValue<string>());
            Assert.AreEqual("Jane Smith", data["Employee"]!["Name"]!.GetValue<string>());
            Assert.AreEqual(28L, data["Employee"]!["Age"]!.GetValue<long>());
        }

        [TestCase(nameof(TestReturnTypeMethods.ReturnList), new[] { 1, 2, 3 })]
        [TestCase(nameof(TestReturnTypeMethods.ReturnArray), new[] { 10, 20, 30, 40 })]
        public void ListsOfValuesAreItems(string method, int[] expected)
        {
            var data = Run(method).data as JsonArray;
            CollectionAssert.AreEqual(expected.Select(i => (long)i).ToArray(), data!.Select(n => n!.GetValue<long>()).ToArray());
        }

        [Test]
        public void ListsOfObjectsAreRows()
        {
            var (_, text, data) = Run(nameof(TestReturnTypeMethods.ReturnListOfCustomObjects));
            StringAssert.Contains("Name|Age", text, "keys once, as a header row");
            var rows = (JsonArray)data!;
            Assert.AreEqual("Alice", rows[0]!["Name"]!.GetValue<string>());
            Assert.AreEqual(35L, rows[1]!["Age"]!.GetValue<long>());
        }

        [Test]
        public void DictionariesKeepTheirKeys()
        {
            var data = Run(nameof(TestReturnTypeMethods.ReturnDictionary)).data;
            Assert.AreEqual("value1", data!["key1"]!.GetValue<string>());
            Assert.AreEqual("value2", data["key2"]!.GetValue<string>());
        }

        [Test]
        public void DictionariesOfObjectsAreWrittenOut()
        {
            var data = Run(nameof(TestReturnTypeMethods.ReturnDictionaryWithComplexValues)).data;
            Assert.AreEqual("Charlie", data!["person1"]!["Name"]!.GetValue<string>());
            Assert.AreEqual(32L, data["person2"]!["Age"]!.GetValue<long>());
        }

        [Test]
        public void AResponseCallToolPassesThrough() =>
            Assert.AreEqual("Custom Response", Run(nameof(TestReturnTypeMethods.ReturnResponseCallTool)).text);
    }
}
