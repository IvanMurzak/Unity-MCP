/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler : BaseTest
    {
        protected Tool_Profiler _tool = null!;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Profiler();
        }

        [TearDown]
        public void TestTearDown()
        {
            // Stop profiler after each test to reset state
            _tool.Stop();
        }

        protected void ResultValidation(string? result)
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result!.Contains("[Success]"), $"Should contain success message.\nResult: {result}");
            Assert.IsFalse(result.Contains("[Error]"), $"Should not contain error message.\nResult: {result}");
        }

        protected void ResultValidationExpected(string? result, params string[] expectedLines)
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result!.Contains("[Success]"), $"Should contain success message.\nResult: {result}");

            foreach (var line in expectedLines)
                Assert.IsTrue(result.Contains(line), $"Should contain expected line: {line}\nResult: {result}");
        }

        protected void ErrorValidation(string? result, string expectedErrorPart = "[Error]")
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Error Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(result!.Contains(expectedErrorPart), $"Should contain error part: {expectedErrorPart}\nResult: {result}");
        }

        protected void StructuredResponseValidation<T>(ResponseCallValueTool<T?> response)
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Structured Response Status: {response.Status}");
            Assert.AreEqual(ResponseStatus.Success, response.Status, $"Response should be successful.");
            Assert.IsNotNull(response.StructuredContent, "Response should have structured content.");
        }

        protected void StructuredResponseErrorValidation<T>(ResponseCallValueTool<T?> response)
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Structured Error Response Status: {response.Status}");
            Assert.AreEqual(ResponseStatus.Error, response.Status, $"Response should be error.");
        }

        protected T? DeserializeStructuredResponse<T>(JsonNode? structuredContent)
        {
            if (structuredContent == null) return default;
            var jsonString = structuredContent.ToJsonString();
            var contentToDeserialize = jsonString;

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty(JsonSchema.Result, out var resultProp))
                {
                    contentToDeserialize = resultProp.GetRawText();
                }
            }
            catch { }

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<T>(contentToDeserialize, options);
        }
    }
}

