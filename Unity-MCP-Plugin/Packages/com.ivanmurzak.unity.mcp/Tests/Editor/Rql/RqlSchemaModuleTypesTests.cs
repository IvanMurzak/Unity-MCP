/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql
{
    /// <summary>
    /// rql-schema resolves the RQL modules' own item types by name (an extension's views such as ActionMapView, SplineView):
    /// the schema prints them as member types, and RQL's reachability walk stops long before them in a Unity project.
    /// </summary>
    public class RqlSchemaModuleTypesTests : Adapters.AdapterTestBase
    {
        [Test]
        public void ModuleItemTypesAreIndexedButNotTheirAdaptersOrModules()
        {
            CollectionAssert.Contains(UnityTypes.Candidates("RqlEditorApi").ToList(), typeof(RqlEditorApi));
            Assert.IsTrue(UnityTypes.IsModuleItemType(typeof(RqlEditorApi)));
            Assert.IsFalse(UnityTypes.IsModuleItemType(typeof(UnityEngine_Material_Adapter)), "an adapter is not agent-facing");
            Assert.IsFalse(UnityTypes.IsModuleItemType(typeof(com.IvanMurzak.Unity.MCP.Editor.Rql.Modules.UnityCoreModule)), "a module is not agent-facing");
            Assert.IsFalse(UnityTypes.IsModuleItemType(typeof(RqlUnity)), "a static class has no members to read");
            Assert.IsEmpty(UnityTypes.Candidates("UnityEngine_Material_Adapter"));
        }
    }
}
