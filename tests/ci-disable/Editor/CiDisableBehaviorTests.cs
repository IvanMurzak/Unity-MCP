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
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;

public class CiDisableBehaviorTests
{
    [Test]
    public void DisabledRestoredWindowCannotInitializeMcp()
    {
        if (Environment.GetEnvironmentVariable("UNITY_MCP_DISABLE") != "1")
            Assert.Ignore("Run in a dedicated editor launched with UNITY_MCP_DISABLE=1.");
        AssertInactive();
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("com.IvanMurzak.Unity.MCP.Editor.UI.MainWindowEditor"))
            .First(t => t != null);
        var window = ScriptableObject.CreateInstance(type!);
        try
        {
            type!.GetMethod("CreateGUI")!.Invoke(window, null);
            AssertInactive();
        }
        finally { UnityEngine.Object.DestroyImmediate(window); }
    }

    [UnityTest]
    public IEnumerator DisabledStartupSurvivesPlayModeDomainReloads()
    {
        if (Environment.GetEnvironmentVariable("UNITY_MCP_DISABLE") != "1")
            Assert.Ignore("Run in a dedicated editor launched with UNITY_MCP_DISABLE=1.");
        AssertInactive();
        yield return new EnterPlayMode();
        AssertInactive();
        yield return new ExitPlayMode();
        AssertInactive();
    }

    static void AssertInactive()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("com.IvanMurzak.Unity.MCP.UnityMcpPluginEditor"))
            .FirstOrDefault(t => t != null);
        Assert.NotNull(type, "This test must exercise loaded MCP assemblies, not excluded ones.");
        Assert.AreEqual(false, type!.GetProperty("HasInstance")!.GetValue(null));
        Assert.False(Resources.FindObjectsOfTypeAll<MonoBehaviour>().Any(o => o.GetType().Name == "MainThreadDispatcher"));
    }
}
