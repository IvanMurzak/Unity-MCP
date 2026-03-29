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
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class SceneViewToolbarOverlayTests : BaseTest
    {
        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var windows = Resources.FindObjectsOfTypeAll<MainWindowEditor>();
            foreach (var w in windows)
                w.Close();

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator ClickCallback_OpensMainWindowEditor()
        {
            MainWindowEditor.ShowWindowVoid();
            yield return null;

            var windows = Resources.FindObjectsOfTypeAll<MainWindowEditor>();
            Assert.That(windows.Length, Is.GreaterThan(0),
                "ShowWindowVoid must open a MainWindowEditor instance");
        }

        [Test]
        public void ButtonId_PrefixedWithOverlayId()
        {
            StringAssert.StartsWith(SceneViewToolbarOverlay.Id, OpenWindowButton.Id);
        }

        [Test]
        public void ButtonId_HasSlashSeparatedActionSuffix()
        {
            var suffix = OpenWindowButton.Id.Substring(SceneViewToolbarOverlay.Id.Length);
            Assert.That(suffix, Does.StartWith("/"), "Unity toolbar requires '/' separator");

            var action = suffix.Substring(1);
            Assert.IsNotEmpty(action, "Action name required after '/'");
        }

        [Test]
        public void OpenWindowButton_SetsExpectedTextAndTooltip()
        {
            var button = new OpenWindowButton();
            Assert.AreEqual("Game Developer", button.text);
            Assert.IsNotEmpty(button.tooltip);
        }

        [Test]
        public void IconAttributePath_StaysInSyncWithRuntimeFallback()
        {
            Assert.AreEqual(EditorAssetLoader.PackageLogoIconPath, EditorAssetLoader.PackageLogoIcon[0],
                "[Icon] attribute const must match the first entry in the runtime fallback array");
        }
    }
}
