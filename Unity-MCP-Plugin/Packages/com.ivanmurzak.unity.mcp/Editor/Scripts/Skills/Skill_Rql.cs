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
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// The RQL skill: the language, the Editor operations, and — generated from the live engine — every source and function
    /// plus the guides that modules contribute. An extension that adds an RQL module (sources, adapters, AddGuide) therefore
    /// appears in this skill as soon as it is installed.
    /// </summary>
    [AiSkillType]
    public static class Skill_Rql
    {
        public const string SkillId = "unity-rql";

        [AiSkill(SkillId,
@"How to read and change anything in the Unity Editor with RQL (rql-query / rql-mutate / rql-schema): the query
language, operators, sources (scene objects, assets, scenes, prefabs, console logs, Editor state), Editor operations
(create, add components, scenes, prefabs, assets, selection, Play Mode) and what installed extensions add.")]
        public static string Markdown => MainThread.Instance.Run(Render);

        public static string Render()
        {
            var engine = RqlUnity.Engine;
            var sb = new StringBuilder();
            sb.Append(@"# RQL — query and change the Unity Editor

RQL is one pipeline language for the whole Editor: find objects, read members, filter, aggregate, and change them in one
call. Prefer one precise query over many exploratory calls.

- `rql-query` — read-only queries, compact table results.
- `rql-mutate` — queries ending with `set` / `call`; all-or-nothing, undoable (Ctrl+Z), `dry_run: true` previews.
- `rql-schema` — sources and component types in the scene; `rql-schema(type)` lists one type's members and methods.

```bash
unity-mcp-cli run-tool rql-query --input '{""query"": ""objects | where name contains \""Enemy\"" | select $ref name""}'
unity-mcp-cli run-tool rql-mutate --input '{""query"": ""objects | where name == \""Player\"" | set active = false""}'
```

## Language

```
").Append(RqlGuide.Language).Append(@"
```

## Changing things (rql-mutate)

```
").Append(RqlGuide.MutateStages).Append(RqlGuide.MutateExamples).Append(@"```

## Sources

| Source | What |
|---|---|
");
            foreach (var s in engine.Sources.Values.OrderBy(s => s.Name, System.StringComparer.Ordinal))
                sb.Append("| `").Append(s.Name).Append("` | ").Append(Cell(s.Description ?? "")).Append(" |\n");
            foreach (var f in engine.Functions.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
                sb.Append("| `").Append(f).Append("(…)` | ").Append(Cell(engine.FunctionDescriptions.TryGetValue(f, out var d) ? d : "")).Append(" |\n");

            foreach (var g in engine.Guides)
                sb.Append("\n## ").Append(g.Key).Append("\n\n").Append(g.Value.Trim()).Append('\n');

            sb.Append("\n## Installed RQL modules\n\n");
            foreach (var m in engine.Modules)
                sb.Append("- `").Append(m.Module).Append('`').Append(m.Ok ? "" : " — failed: " + m.Error).Append('\n');
            return sb.ToString();
        }

        private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");
    }
}
