/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    /// <summary>The views the asset's schema names (maps IReadOnlyList&lt;ActionMapView&gt; …) are described by name too.</summary>
    public class InputViewSchemaTests : InputSystemTestBase
    {
        [Test]
        public void TheViewTypesHaveASchema()
        {
            foreach (var view in new[] { "ActionMapView", "ActionView", "BindingView", "ControlSchemeView" })
                StringAssert.StartsWith(view + " {", RqlUnitySchema.Render(RqlUnity.Engine, view));
            StringAssert.Contains("AddAction(", RqlUnitySchema.Render(RqlUnity.Engine, "ActionMapView"));
        }
    }
}
