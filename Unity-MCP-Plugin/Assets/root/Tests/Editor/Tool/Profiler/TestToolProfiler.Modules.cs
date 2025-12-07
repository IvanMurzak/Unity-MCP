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
using System.Collections;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [Test]
        public void EnableModule_WithEmptyName_ReturnsError()
        {
            // Act
            var result = _tool.EnableModule(string.Empty);

            // Assert
            ErrorValidation(result, "Module name is required");
        }

        [Test]
        public void EnableModule_WithNullName_ReturnsError()
        {
            // Act
            var result = _tool.EnableModule(null!);

            // Assert
            ErrorValidation(result, "Module name is required");
        }

        [Test]
        public void EnableModule_WithInvalidName_ReturnsError()
        {
            // Act
            var result = _tool.EnableModule("InvalidModuleName");

            // Assert
            ErrorValidation(result, "Unknown profiler module");
        }

        [Test]
        public void EnableModule_WithValidName_EnablesModule()
        {
            // Arrange
            var moduleName = "CPU";

            // Act
            var result = _tool.EnableModule(moduleName, enabled: true);

            // Assert
            ResultValidationExpected(result, moduleName, "enabled");
        }

        [Test]
        public void EnableModule_WithValidName_DisablesModule()
        {
            // Arrange
            var moduleName = "GPU";

            // Act
            var result = _tool.EnableModule(moduleName, enabled: false);

            // Assert
            ResultValidationExpected(result, moduleName, "disabled");
        }

        [Test]
        public void EnableModule_AllAvailableModules_AreValid()
        {
            // Test all available modules
            foreach (var moduleName in Tool_Profiler.AvailableModules)
            {
                // Act
                var result = _tool.EnableModule(moduleName, enabled: true);

                // Assert
                ResultValidation(result);
            }
        }

        [UnityTest]
        public IEnumerator ListModules_ReturnsAllModules()
        {
            // Act
            var response = _tool.ListModules();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerModulesData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsNotNull(data!.Modules, "Modules should not be null.");
            Assert.AreEqual(Tool_Profiler.AvailableModules.Count, data.TotalModules, 
                "TotalModules should match available modules count.");
        }

        [UnityTest]
        public IEnumerator ListModules_ReturnsModuleNames()
        {
            // Act
            var response = _tool.ListModules();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerModulesData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsNotNull(data!.Modules, "Modules should not be null.");

            foreach (var module in data.Modules!)
            {
                Assert.IsNotNull(module.Name, "Module name should not be null.");
                Assert.IsNotEmpty(module.Name, "Module name should not be empty.");
                Assert.IsTrue(Tool_Profiler.AvailableModules.Contains(module.Name!),
                    $"Module '{module.Name}' should be in available modules list.");
            }
        }

        [UnityTest]
        public IEnumerator ListModules_ReturnsEnabledCount()
        {
            // Act
            var response = _tool.ListModules();
            yield return null;

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerModulesData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.GreaterOrEqual(data!.EnabledCount, 0, "EnabledCount should be >= 0.");
            Assert.LessOrEqual(data.EnabledCount, data.TotalModules, 
                "EnabledCount should be <= TotalModules.");
        }

        [UnityTest]
        public IEnumerator EnableModule_ThenListModules_ReflectsChange()
        {
            // Arrange - disable a module
            _tool.EnableModule("VirtualTexturing", enabled: false);
            yield return null;

            // Act
            var response = _tool.ListModules();

            // Assert
            StructuredResponseValidation(response);

            var data = DeserializeStructuredResponse<Tool_Profiler.ProfilerModulesData>(response.StructuredContent);
            Assert.IsNotNull(data, "Data should not be null.");
            Assert.IsNotNull(data!.Modules, "Modules should not be null.");

            var vtModule = data.Modules!.FirstOrDefault(m => m.Name == "VirtualTexturing");
            Assert.IsNotNull(vtModule, "VirtualTexturing module should be in list.");
            Assert.IsFalse(vtModule!.Enabled, "VirtualTexturing should be disabled.");

            // Re-enable
            _tool.EnableModule("VirtualTexturing", enabled: true);
            yield return null;

            response = _tool.ListModules();
            data = DeserializeStructuredResponse<Tool_Profiler.ProfilerModulesData>(response.StructuredContent);
            vtModule = data!.Modules!.FirstOrDefault(m => m.Name == "VirtualTexturing");
            Assert.IsTrue(vtModule!.Enabled, "VirtualTexturing should be enabled after re-enabling.");
        }

        [Test]
        public void Error_ProfilerNotEnabled_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Profiler.Error.ProfilerNotEnabled();

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("Profiler must be enabled"), "Should contain error description.");
        }

        [Test]
        public void Error_ModuleNameIsRequired_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Profiler.Error.ModuleNameIsRequired();

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("Module name is required"), "Should contain error description.");
        }

        [Test]
        public void Error_UnknownModule_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Profiler.Error.UnknownModule("TestModule");

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("Unknown profiler module"), "Should contain error description.");
            Assert.IsTrue(result.Contains("'TestModule'"), "Should contain the module name.");
        }

        [Test]
        public void Error_FilePathIsRequired_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Profiler.Error.FilePathIsRequired();

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("File path is required"), "Should contain error description.");
        }

        [Test]
        public void Error_FileNotFound_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Profiler.Error.FileNotFound("/path/to/file.json");

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("file not found"), "Should contain error description.");
            Assert.IsTrue(result.Contains("/path/to/file.json"), "Should contain the file path.");
        }
    }
}

