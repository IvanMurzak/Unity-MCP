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

        // Wave 6: asked to "turn on every extension", an agent spent 60 turns reflecting over assemblies: the overview now
        // names the installed extension packages (always active) and what each adds, and rql-schema "guide:<topic>" prints a
        // module's guide.
        [Test]
        public void TheOverviewListsInstalledExtensionsAndGuides()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            var modules = UnityEditor.TypeCache.GetTypesDerivedFrom<global::Rql.Execution.IRqlModule>();
            var extensionPackages = modules
                .Select(t => UnityEditor.PackageManager.PackageInfo.FindForAssembly(t.Assembly))
                .Where(p => p != null && p.name.StartsWith("com.ivanmurzak.unity.mcp.", System.StringComparison.Ordinal))
                .Select(p => p!.name).Distinct().ToList();
            if (extensionPackages.Count == 0)
                StringAssert.DoesNotContain("extensions (installed packages", overview);
            else
            {
                StringAssert.Contains("extensions (installed packages, always active: nothing to turn on):", overview);
                foreach (var package in extensionPackages) StringAssert.Contains(package, overview);
            }
            foreach (var guide in RqlUnity.Engine.Guides) StringAssert.Contains(guide.Key, overview);

            var engine = new global::Rql.Execution.RqlEngine();
            engine.AddGuide("Test Topic", "use `objects | where …`");
            StringAssert.StartsWith("guide Test Topic:\nuse `objects", RqlUnitySchema.Render(engine, "guide:test topic"));
            StringAssert.StartsWith("error unknown-guide: no guide 'Nope' — guides: Test Topic", RqlUnitySchema.Render(engine, "guide: Nope"));
            StringAssert.Contains("guide:<topic>", RqlGuide.SchemaDescription);
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
