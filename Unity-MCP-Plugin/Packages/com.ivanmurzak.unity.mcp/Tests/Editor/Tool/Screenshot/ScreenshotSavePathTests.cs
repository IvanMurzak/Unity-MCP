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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AIGD;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The <c>savePath</c> rules, with no rendering: these run in every editor, -nographics included. What a real capture does with
    /// the path is in <see cref="ScreenshotSavePathCaptureTests"/>.
    /// </summary>
    public class ScreenshotSavePathTests : BaseTest
    {
        // A valid 1x1 PNG, so that an import into Assets/ is a real texture import.
        internal static readonly byte[] TinyPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

        string _root = string.Empty;

        [SetUp]
        public void CreateFakeProjectRoot()
        {
            _root = ProjectFolder("Temp/SavePathTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void DeleteFakeProjectRoot()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void CheckSavePath_NullOrBlank_AsksForNoSave()
        {
            foreach (var blank in new[] { null, "", "   " })
            {
                Assert.IsNull(Tool_Screenshot.CheckSavePath(blank, _root, out var full), $"'{blank}'");
                Assert.IsNull(full, $"'{blank}'");
            }
        }

        [Test]
        public void CheckSavePath_InsideTheProject_ResolvesToCleanAbsolutePath()
        {
            var rows = new Dictionary<string, string>
            {
                ["Screenshots/shot.png"] = "Screenshots/shot.png",
                ["Screenshots\\shot.PNG"] = "Screenshots/shot.PNG",
                ["./shot.png"] = "shot.png",
                ["Screenshots/./shot.png"] = "Screenshots/shot.png",
                ["Screenshots//shot.png"] = "Screenshots/shot.png",
                ["Screenshots/Sub/../shot.png"] = "Screenshots/shot.png",
                ["Screenshots/shot"] = "Screenshots/shot.png",
                ["Assets/Deep/Er/shot.png"] = "Assets/Deep/Er/shot.png",
                ["  shot.png  "] = "shot.png",
                ["Library.png"] = "Library.png",
                [_root + "/Abs/shot.png"] = "Abs/shot.png",
                [_root + "//Abs//shot.png"] = "Abs/shot.png",
            };
            foreach (var row in rows)
            {
                Assert.IsNull(Tool_Screenshot.CheckSavePath(row.Key, _root, out var full), row.Key);
                Assert.AreEqual(_root + "/" + row.Value, full, row.Key);
            }
        }

        [Test]
        public void CheckSavePath_Refused_SaysWhyAndGivesNoFile()
        {
            var refused = new[]
            {
                // Outside the project
                "../shot.png", "..\\shot.png", "Screenshots/../../shot.png", ".. /shot.png", "Screenshots/.../shot.png", "Foo./shot.png",
                _root + "/../shot.png", _root + "-sibling/shot.png", Path.GetDirectoryName(_root)!.Replace('\\', '/') + "/shot.png",
                "/shot.png", "C:shot.png", "C:/elsewhere/shot.png", "\\\\server\\share\\shot.png", "//server/share/shot.png", "~/shot.png",
                "\\\\?\\C:\\shot.png", "folder:stream/shot.png", _root + "/folder:stream/shot.png",
                // Each of these names a folder, whatever a path API would collapse it to
                ".", "./", "./.", ".//.", ".\\", "..", "../", "Screenshots/.", "Screenshots/./", "Screenshots//", "Screenshots//.",
                "Screenshots/..", "Screenshots/Sub/..", "Screenshots/", _root, _root + "/", _root + "/.", _root + "/Screenshots/.",
                // No file name
                ".png", "..png", "Screenshots/..png",
                // Not a PNG, or a name Windows treats differently from how it reads
                "shot.jpg", "shot.png.jpg", "shot.png:stream", "shot.png.", "shot.png ./x", "sh*ot.png", "sh?ot.png", "NUL.png", "COM1/shot.png",
                // Unity's own folders
                "Library/shot.png", "library/shot.png", "Temp/shot.png", "Logs/a/shot.png", "obj/shot.png", "Packages/shot.png",
                "ProjectSettings/shot.png", "UserSettings/shot.png", _root + "/Temp/shot.png", "Assets/../Temp/shot.png",
            };
            foreach (var path in refused)
            {
                var refusal = Tool_Screenshot.CheckSavePath(path, _root, out var full);
                Assert.IsNotNull(refusal, $"'{path}' should be refused, resolved to '{full}'");
                StringAssert.StartsWith("Cannot save to", refusal, path);
                Assert.IsNull(full, path);
            }
            Assert.IsEmpty(Directory.GetFileSystemEntries(_root), "A refusal must not leave anything behind");
        }

        [Test]
        public void CheckSavePath_ExistingFolderWithoutExtension_IsRefusedAsFolder()
        {
            Directory.CreateDirectory(_root + "/Screenshots");

            var refusal = Tool_Screenshot.CheckSavePath("Screenshots", _root, out _);

            StringAssert.Contains("names a folder", refusal);
        }

        [Test]
        public void CheckSavePath_OtherCaseOfAnExistingFolder_UsesTheSpellingOnDisk()
        {
            Directory.CreateDirectory(_root + "/Assets/Textures");
            var expected = Directory.Exists(_root + "/assets") // the file system ignores case, as on Windows and by default on macOS
                ? _root + "/Assets/Textures/shot.png"
                : _root + "/assets/textures/shot.png";

            Assert.IsNull(Tool_Screenshot.CheckSavePath("assets/textures/shot.png", _root, out var full));

            Assert.AreEqual(expected, full);
        }

        [Test]
        public void CaptureResult_UnderAFolderUnityIgnores_SaysItStaysAPlainFile()
        {
            var folder = ProjectFolder("Assets/McpSavePathTests_" + Guid.NewGuid().ToString("N") + "~");
            try
            {
                var full = folder + "/shot.png";

                var result = Tool_Screenshot.CaptureResult(TinyPng, "caption", full);

                AssertSaved(result, full);
                StringAssert.Contains("stays a plain file", TextOf(result));
                Assert.IsFalse(File.Exists(full + ".meta"));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        [Test]
        public void CheckSavePath_ExistingFile_IsRefused()
        {
            Directory.CreateDirectory(_root + "/Screenshots");
            File.WriteAllBytes(_root + "/Screenshots/shot.png", TinyPng);

            foreach (var path in new[] { "Screenshots/shot.png", "Screenshots/shot" })
                StringAssert.Contains("never overwrites", Tool_Screenshot.CheckSavePath(path, _root, out _), path);
        }

        [Test]
        public void CheckSavePath_LinkInThePath_IsRefused()
        {
            var outside = _root + "-outside";
            Directory.CreateDirectory(outside);
            try
            {
                CreateDirectoryLink(_root + "/Link", outside);

                foreach (var path in new[] { "Link/shot.png", "Link/Sub/shot.png", _root + "/Link/shot.png" })
                    StringAssert.Contains("is a link", Tool_Screenshot.CheckSavePath(path, _root, out _), path);
                Assert.IsEmpty(Directory.GetFileSystemEntries(outside));
            }
            finally
            {
                // Link first: a recursive delete of the link's parent must never reach through to the target.
                if (Directory.Exists(_root + "/Link"))
                    Directory.Delete(_root + "/Link");
                Directory.Delete(outside, recursive: true);
            }
        }

        [Test]
        public void CaptureResult_NoSavePath_IsThePlainImage()
        {
            var result = Tool_Screenshot.CaptureResult(TinyPng, "caption", saveTo: null);

            Assert.AreEqual(ResponseStatus.Success, result.Status);
            Assert.AreEqual("caption", TextOf(result));
            Assert.IsNotNull(ImageOf(result));
        }

        [Test]
        public void CaptureResult_OutsideAssets_WritesPlainFileWithNoMeta()
        {
            var folder = ProjectFolder("McpSavePathTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                var full = folder + "/New/Sub/shot.png";

                var result = Tool_Screenshot.CaptureResult(TinyPng, "caption", full);

                AssertSaved(result, full);
                Assert.IsFalse(File.Exists(full + ".meta"));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        [Test]
        public void CaptureResult_UnderAssets_ImportsTheFileInANewFolder()
        {
            var folder = "Assets/McpSavePathTests_" + Guid.NewGuid().ToString("N");
            try
            {
                var assetPath = folder + "/Sub/shot.png";
                var full = ProjectFolder(assetPath);

                var result = Tool_Screenshot.CaptureResult(TinyPng, "caption", full);

                AssertSaved(result, full);
                StringAssert.Contains("Imported as the Unity asset " + assetPath, TextOf(result));
                Assert.IsTrue(File.Exists(full + ".meta"), "The import should create the .meta");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath), "The file should load as an asset straight away");
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void CaptureResult_ExistingFile_IsNeverOverwritten()
        {
            // What decides this is the write itself: the file can appear after the path was checked, during the capture.
            var full = _root + "/shot.png";
            var original = new byte[] { 1, 2, 3 };
            File.WriteAllBytes(full, original);

            var result = Tool_Screenshot.CaptureResult(TinyPng, "caption", full);

            Assert.AreEqual(ResponseStatus.Error, result.Status);
            StringAssert.Contains("never overwrites", TextOf(result));
            CollectionAssert.AreEqual(original, File.ReadAllBytes(full));
        }

        // Stays in the JSON path an agent uses, and runs without a device: a bad path comes back before any graphics check.
        [TestCase(Tool_Screenshot.ScreenshotCameraToolId, "")]
        [TestCase(Tool_Screenshot.ScreenshotGameViewToolId, "")]
        [TestCase(Tool_Screenshot.ScreenshotSceneViewToolId, "")]
        [TestCase(Tool_Screenshot.ScreenshotIsolatedToolId, @"""gameObjectRef"": { ""name"": ""__missing__"" }, ")]
        public void Tool_BadSavePath_RefusesBeforeCapturing(string toolId, string otherArguments)
        {
            var savedDevice = Tool_Screenshot.NoGraphicsDevice;
            Tool_Screenshot.NoGraphicsDevice = true; // Reaching the graphics check would answer "-nographics" instead
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var raw = RunToolRaw(toolId, $@"{{ {otherArguments}""savePath"": ""../outside.png"" }}");

                StringAssert.Contains("Cannot save to", raw);
                StringAssert.Contains("inside the project folder", raw);
                StringAssert.DoesNotContain("-nographics", raw);
                Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(UnityMcpPluginEditor.ProjectRootPath)!, "outside.png")));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Tool_Screenshot.NoGraphicsDevice = savedDevice;
            }
        }

        [TestCase(nameof(Tool_Screenshot.ScreenshotCamera))]
        [TestCase(nameof(Tool_Screenshot.ScreenshotGameView))]
        [TestCase(nameof(Tool_Screenshot.ScreenshotSceneView))]
        [TestCase(nameof(Tool_Screenshot.ScreenshotIsolated))]
        public void Tool_CanWrite_SoItsHintsDoNotClaimOtherwise(string methodName)
        {
            var method = typeof(Tool_Screenshot).GetMethod(methodName)!;
            var tool = method.GetCustomAttribute<AiToolAttribute>()!;

            Assert.AreEqual(false, tool.ReadOnlyHintValue, "It can write a file");
            Assert.AreEqual(false, tool.IdempotentHintValue, "The same savePath is refused the second time");
            Assert.AreEqual(false, tool.DestructiveHintValue, "It never overwrites");

            var savePath = method.GetParameters().Single(p => p.Name == "savePath");
            Assert.IsTrue(savePath.IsOptional, "savePath must stay optional");
            Assert.IsNull(savePath.DefaultValue);
            StringAssert.Contains("wants the picture as a file", savePath.GetCustomAttribute<DescriptionAttribute>()!.Description);
            StringAssert.Contains("savePath", method.GetCustomAttribute<DescriptionAttribute>()!.Description);
        }

        internal static string ProjectFolder(string relative) =>
            UnityMcpPluginEditor.ProjectRootPath.Replace('\\', '/') + "/" + relative;

        internal static string TextOf(ResponseCallTool result) =>
            string.Join("\n", result.Content.Where(c => c.Text != null).Select(c => c.Text));

        internal static string? ImageOf(ResponseCallTool result) =>
            result.Content.FirstOrDefault(c => c.Data != null)?.Data;

        // The file holds the very image the tool returned.
        internal static void AssertSaved(ResponseCallTool result, string full)
        {
            Assert.AreEqual(ResponseStatus.Success, result.Status, TextOf(result));
            StringAssert.Contains("Saved to " + full, TextOf(result));
            var image = ImageOf(result);
            Assert.IsNotNull(image, "The image must still be returned");
            Assert.IsTrue(File.Exists(full), "The file must exist");
            CollectionAssert.AreEqual(Convert.FromBase64String(image!), File.ReadAllBytes(full));
        }

        static void CreateDirectoryLink(string link, string target)
        {
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            var start = isWindows
                ? new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link.Replace('/', '\\')}\" \"{target.Replace('/', '\\')}\"")
                : new ProcessStartInfo("/bin/ln", $"-s \"{target}\" \"{link}\"");
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            using var process = Process.Start(start)!;
            process.WaitForExit();
            if (process.ExitCode != 0)
                Assert.Fail($"Could not create the test link: {process.StandardError.ReadToEnd()}");
        }
    }

    /// <summary>A real capture through each tool, so these need a device. They run in the CI "Graphics" leg; see <see cref="RequiresGraphicsDeviceAttribute"/>.</summary>
    [RequiresGraphicsDevice]
    public class ScreenshotSavePathCaptureTests : BaseTest
    {
        string _folder = string.Empty;
        string _relative = string.Empty;
        string _full = string.Empty;

        Tool_Screenshot Tool => new Tool_Screenshot();

        [SetUp]
        public void ChooseTarget()
        {
            var name = "McpSavePathTests_" + Guid.NewGuid().ToString("N");
            _folder = ScreenshotSavePathTests.ProjectFolder(name);
            _relative = name + "/shot.png";
            _full = _folder + "/shot.png";
        }

        [TearDown]
        public void DeleteTarget()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, recursive: true);
        }

        static GameObjectRef CreateCube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SavePathCube";
            return new GameObjectRef { Name = cube.name };
        }

        static void CreateCamera() => new GameObject("SavePathCamera").AddComponent<Camera>();

        static void OpenGameView()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor")!;
            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            gameView.Show();
            gameView.Focus();
            gameView.Repaint();
        }

        static void OpenSceneView()
        {
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>("Scene", true);
            sceneView.Show();
            sceneView.Focus();
            sceneView.Repaint();
        }

        void AssertSavedThenNotOverwritten(Func<ResponseCallTool> capture)
        {
            ScreenshotSavePathTests.AssertSaved(capture(), _full);

            var bytes = File.ReadAllBytes(_full);
            var second = capture();
            Assert.AreEqual(ResponseStatus.Error, second.Status);
            StringAssert.Contains("never overwrites", ScreenshotSavePathTests.TextOf(second));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(_full));
        }

        void AssertDefaultUnchanged(ResponseCallTool result)
        {
            Assert.AreEqual(ResponseStatus.Success, result.Status, ScreenshotSavePathTests.TextOf(result));
            Assert.IsNotNull(ScreenshotSavePathTests.ImageOf(result));
            StringAssert.DoesNotContain("Saved to", ScreenshotSavePathTests.TextOf(result));
            Assert.IsFalse(Directory.Exists(_folder));
        }

        [Test]
        public void ScreenshotCamera_SavePath_WritesAndDoesNotOverwrite()
        {
            CreateCamera();
            AssertSavedThenNotOverwritten(() => Tool.ScreenshotCamera(width: 64, height: 64, savePath: _relative));
        }

        [Test]
        public void ScreenshotCamera_NoSavePath_IsUnchanged()
        {
            CreateCamera();
            AssertDefaultUnchanged(Tool.ScreenshotCamera(width: 64, height: 64));
        }

        [Test]
        public void ScreenshotCamera_ViaRunTool_WritesTheFile()
        {
            CreateCamera();

            RunTool(Tool_Screenshot.ScreenshotCameraToolId, $@"{{ ""width"": 64, ""height"": 64, ""savePath"": ""{_relative}"" }}");

            Assert.IsTrue(File.Exists(_full));
        }

        [Test]
        public void ScreenshotSceneView_SavePath_WritesAndDoesNotOverwrite()
        {
            OpenSceneView();
            AssertSavedThenNotOverwritten(() => Tool.ScreenshotSceneView(width: 64, height: 64, savePath: _relative));
        }

        [Test]
        public void ScreenshotSceneView_NoSavePath_IsUnchanged()
        {
            OpenSceneView();
            AssertDefaultUnchanged(Tool.ScreenshotSceneView(width: 64, height: 64));
        }

        [Test]
        public void ScreenshotGameView_SavePath_WritesAndDoesNotOverwrite()
        {
            OpenGameView();
            AssertSavedThenNotOverwritten(() => Tool.ScreenshotGameView(savePath: _relative));
        }

        [Test]
        public void ScreenshotGameView_NoSavePath_IsUnchanged()
        {
            OpenGameView();
            AssertDefaultUnchanged(Tool.ScreenshotGameView());
        }

        [Test]
        public void ScreenshotIsolated_SavePath_WritesAndDoesNotOverwrite()
        {
            var cube = CreateCube();
            AssertSavedThenNotOverwritten(() => Tool.ScreenshotIsolated(gameObjectRef: cube, resolution: 64, savePath: _relative));
        }

        [Test]
        public void ScreenshotIsolated_Composite_SavePath_Writes()
        {
            var cube = CreateCube();
            var result = Tool.ScreenshotIsolated(gameObjectRef: cube, cameraView: Tool_Screenshot.CameraView.Composite, resolution: 32, savePath: _relative);
            ScreenshotSavePathTests.AssertSaved(result, _full);
        }

        [Test]
        public void ScreenshotIsolated_NoSavePath_IsUnchanged()
        {
            var cube = CreateCube();
            AssertDefaultUnchanged(Tool.ScreenshotIsolated(gameObjectRef: cube, resolution: 64));
        }
    }
}
