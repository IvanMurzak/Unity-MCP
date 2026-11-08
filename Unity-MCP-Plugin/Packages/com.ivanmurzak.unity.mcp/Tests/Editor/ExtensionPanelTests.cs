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
using System.IO;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ExtensionPanelTests
    {
        private const string Unity60003 = "6000.3.1f1";
        private const string Unity60006 = "6000.6.4f1";
        private const string Animation = "com.ivanmurzak.unity.mcp.animation";
        private const string InputSystem = "com.ivanmurzak.unity.mcp.inputsystem";

        private static string[] Specs(JsonObject? dependencies, string unityVersion, string packageId, string installed, string? latest)
            => UpdateChecker.BuildExtensionUpdateSpecs(new[] { (packageId, installed, latest) }, dependencies, unityVersion);

        [Test]
        public void BuildExtensionUpdateSpecs_RegistryExtensionBehindLatest_IsUpdated()
        {
            var specs = Specs(null, Unity60003, Animation, "2.0.0", "2.1.1");

            Assert.AreEqual(new[] { $"{Animation}@2.1.1" }, specs);
        }

        [TestCase("2.1.1", "2.1.1")]
        [TestCase("2.2.0", "2.1.1")]
        [TestCase("2.1.1", null)]
        public void BuildExtensionUpdateSpecs_NothingNewerKnown_IsSkipped(string installed, string? latest)
        {
            var specs = Specs(null, Unity60003, Animation, installed, latest);

            Assert.IsEmpty(specs);
        }

        [Test]
        public void BuildExtensionUpdateSpecs_UnityPackageFloorNotMet_IsBroughtAlong()
        {
            var dependencies = new JsonObject { ["com.unity.inputsystem"] = "1.17.0" };

            var specs = Specs(dependencies, Unity60006, InputSystem, "2.0.0", "2.1.1");

            Assert.AreEqual(new[] { $"{InputSystem}@2.1.1", "com.unity.inputsystem@1.19.0" }, specs);
        }

        [Test]
        public void BuildExtensionUpdateSpecs_UnityPackageFloorMet_IsNotDowngraded()
        {
            var dependencies = new JsonObject { ["com.unity.inputsystem"] = "1.20.0" };

            var specs = Specs(dependencies, Unity60006, InputSystem, "2.0.0", "2.1.1");

            Assert.AreEqual(new[] { $"{InputSystem}@2.1.1" }, specs);
        }

        [Test]
        public void AddToManifest_SeveralExtensions_KeepsEachOneAndRegistry()
        {
            var manifestPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(manifestPath, "{\"dependencies\":{\"com.unity.ugui\":\"2.0.0\"}}");

                Assert.IsTrue(ExtensionPanel.AddToManifest(manifestPath, Animation, "2.1.1", Unity60003));
                Assert.IsTrue(ExtensionPanel.AddToManifest(manifestPath, InputSystem, "2.1.1", Unity60006));

                var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
                Assert.AreEqual("2.1.1", manifest["dependencies"]![Animation]!.GetValue<string>());
                Assert.AreEqual("2.1.1", manifest["dependencies"]![InputSystem]!.GetValue<string>());
                Assert.AreEqual("1.19.0", manifest["dependencies"]!["com.unity.inputsystem"]!.GetValue<string>());
                Assert.AreEqual("2.0.0", manifest["dependencies"]!["com.unity.ugui"]!.GetValue<string>());
                Assert.AreEqual(1, manifest["scopedRegistries"]!.AsArray().Count);
            }
            finally
            {
                File.Delete(manifestPath);
            }
        }
    }
}
