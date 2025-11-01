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
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.RefTypes
{
    public partial class AssetObjectRefTests : BaseRefTests
    {
        [Test]
        public void InstanceID_Description_Includes_All_Relevant_Props() => DescriptionContainsKeywords(
            prop: typeof(AssetObjectRef).GetProperty(nameof(AssetObjectRef.InstanceID)),
            expectedKeywords: AssetObjectRef.AssetObjectRefProperty.All
                .Where(name => name != ObjectRef.ObjectRefProperty.InstanceID));
    }
}
