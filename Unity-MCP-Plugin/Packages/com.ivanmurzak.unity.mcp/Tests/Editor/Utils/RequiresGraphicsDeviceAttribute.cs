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
    /// graphics device (<c>-nographics</c>). CI runs the EditMode suite with <c>-nographics</c>, where a new scene costs milliseconds
    /// instead of seconds of software rendering, and these tests in a separate leg with a graphics device.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
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
