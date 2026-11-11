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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Config = com.IvanMurzak.Unity.MCP.UnityMcpPlugin.UnityConnectionConfig;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Revision 1 of the tool defaults: the Package Manager tools, hidden by default before, are shown once in a project
    /// whose config saved them hidden; after that a user's choice sticks.
    /// </summary>
    public class ToolDefaultsMigrationTests
    {
        static Config WithTools(params (string name, bool visible)[] tools)
            => new Config { Tools = tools.Select(t => new Config.McpFeature(t.name, t.visible)).ToList() };

        static bool Visible(Config c, string name) => c.Tools.Single(t => t.Name == name).Visible;

        [Test]
        public void ShowsThePackageToolsOfAnOldConfig()
        {
            var config = WithTools(("package-add", false), ("package-search", false), ("script-read", false));

            Assert.IsTrue(config.MigrateToolDefaults());
            Assert.IsTrue(Visible(config, "package-add"));
            Assert.IsTrue(Visible(config, "package-search"));
            Assert.IsFalse(Visible(config, "script-read"), "other hidden tools stay hidden");
            Assert.AreEqual(Config.CurrentToolDefaultsRevision, config.ToolDefaultsRevision);
        }

        [Test]
        public void RunsOnceSoAUserCanHideThemAgain()
        {
            var config = WithTools(("package-add", false));
            Assert.IsTrue(config.MigrateToolDefaults());
            config.Tools[0].Visible = false; // the user hides it afterwards

            Assert.IsFalse(config.MigrateToolDefaults());
            Assert.IsFalse(Visible(config, "package-add"));
        }

        [Test]
        public void AFileSavedBeforeTheFieldExistedIsRevisionZero()
        {
            var old = JsonSerializer.Deserialize<Config>("{\"tools\":[{\"name\":\"package-add\",\"visible\":false}]}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            Assert.AreEqual(0, old.ToolDefaultsRevision);
            Assert.IsTrue(old.MigrateToolDefaults());
            Assert.IsTrue(Visible(old, "package-add"));
        }
    }
}
