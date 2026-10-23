/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// A test that renders (screenshots, editor views): in the <see cref="Category"/> category, and ignored in an editor without a
    /// graphics device. CI runs EditMode with <c>-nographics</c> and this category in a leg with the renderer (test_unity_plugin.yml).
    /// This attribute is the only thing that sends a test there: an untagged test that renders without a device may read a blank image.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresGraphicsDeviceAttribute : NUnitAttribute, IApplyToTest
    {
        public const string Category = "Graphics";

        public void ApplyToTest(Test test)
        {
            test.Properties.Add(PropertyNames.Category, Category);
            if (test.RunState == RunState.NotRunnable || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                return;
            test.RunState = RunState.Ignored;
            test.Properties.Set(PropertyNames.SkipReason, "needs a graphics device; the editor runs with -nographics");
        }
    }
}
