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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// A test that captures an editor window (Game View, Scene View): ignored in a headless (<c>-batchmode</c>) editor, which
    /// never draws them, so their tools answer with an error there (ScreenshotHeadlessTests). Pair it with
    /// <see cref="RequiresGraphicsDeviceAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresWindowedEditorAttribute : NUnitAttribute, IApplyToTest
    {
        public void ApplyToTest(Test test)
        {
            if (test.RunState == RunState.NotRunnable || !Application.isBatchMode)
                return;
            test.RunState = RunState.Ignored;
            test.Properties.Set(PropertyNames.SkipReason, "needs a windowed editor; a -batchmode editor never draws its editor windows");
        }
    }
}
