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

        [System.Serializable]
        public class RqlSchemaRowProbe { public int Price; public string Category = ""; }

        // A ScriptableObject's list of [Serializable] rows: get rows reaches them, so rql-schema must know the row type.
        [Test]
        public void SerializableRowTypesHaveASchema()
        {
            var text = RqlUnitySchema.Render(RqlUnity.Engine, "RqlSchemaRowProbe");
            StringAssert.StartsWith("RqlSchemaRowProbe", text);
            StringAssert.Contains("Price int", text);
            StringAssert.Contains("Category string", text);
            Assert.IsEmpty(UnityTypes.Candidates("RqlSchemaRowProbe"), "not an addable / creatable type");
        }

        // Wave 4: agents fell back to code for a tag (AddTag was undocumented), the default material and data-asset rows.
        [Test]
        public void TheGuideShowsTagsBuiltInMaterialsAndDataRows()
        {
            StringAssert.Contains("editor | call AddTag(", RqlGuide.MutateDescription);
            StringAssert.Contains("set sharedMaterial = \"Default-Material\"", RqlGuide.MutateDescription);
            StringAssert.Contains("| get rows |", RqlGuide.MutateDescription);
            StringAssert.Contains("AddTag", RqlUnitySchema.Render(RqlUnity.Engine, null));
        }
    }
}
