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
namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// Agent-facing texts. The language section is the evaluated prompt v4 (RQL evaluation, RESULTS.md) adapted to Unity:
    /// the AI Game Dev App cannot carry a system prompt for one tool, so the guide travels in the tool description, which
    /// the agent loads (ToolSearch) before first use; the one-line blurbs are what the always-visible tool catalog shows.
    /// </summary>
    internal static class RqlGuide
    {
        public const string QueryBlurb =
            "Query the live Unity scene (GameObjects, components, assets) with RQL in ONE call: filter, project, sort, count, " +
            "walk hierarchies. Far fewer calls/tokens than reading objects one by one.";

        public const string MutateBlurb =
            "Change the live Unity scene with RQL set/call: bulk edits in one all-or-nothing, undoable call, saved to disk; dry_run previews.";

        public const string SchemaBlurb =
            "List RQL sources and the component types present in the loaded scenes; pass a type name for its members.";

        public const string Language =
            "RQL: source | stage | stage ...\n" +
            "Sources: objects (every GameObject in the loaded scenes, inactive included), roots, scenes, selection, prefab (the prefab open " +
            "in prefab editing mode), console (Editor log entries), shaders, componentTypes, profiler (memory / rendering / frame " +
            "snapshots; call Start / Stop / Save), tests (test methods; run a selection with tests-run query), testResults (last run), " +
            "editor (Editor state and operations). " +
            "Assets: unity.assets(\"t:Material\") or unity.assets(\"t:Prefab\", \"Assets/Enemies\"), unity.asset(\"Assets/Mats/Red.mat\"). " +
            "ref(\"Light#1234\") resolves a $ref; unity.find(\"Player\") or unity.find(\"Level/Pillar_1\") a GameObject by unique name or path.\n" +
            "Stages:\n" +
            "  where <predicate>                  filter\n" +
            "  select a b.c alias=(expr)          project columns (wrap computed expressions in parentheses)\n" +
            "  sort <expr> [desc], ...            stable sort\n" +
            "  first N | skip N | count\n" +
            "  get <path>                         replace each item by a member (lists are flattened)\n" +
            "  recurse children [max-depth=N] [include-self=false]   walk the hierarchy (input items included by default)\n" +
            "  distinct [expr] | group-by expr | sum expr | min expr | max expr | avg expr\n" +
            "Expressions: == != < <= > >= and or not + - * / %; contains, starts-with, ends-with (case-insensitive); " +
            "matches \"regex\"; x in [a, b]; x is null; x is not null; $ is Light (includes subclasses); has(member). " +
            "Literals: numbers, \"strings\", true/false/null, lists [a, b], objects {x: 1, y: 2}.\n" +
            "Paths: a.b.c, list[0], map[\"key\"]; .member on a list maps over its elements. List methods: .any(pred) .all(pred) " +
            ".count([pred]) .sum(e) .min(e) .max(e) .first([pred]); inside them bare names refer to the element, falling back to the outer item.\n" +
            "Meta: $ (current item), $ref (stable id such as Light#1234), $type, $path, $index, $depth. Enums compare to strings (type == \"Spot\"); " +
            "== on strings is case-sensitive.\n" +
            "Unity: GameObject has components (list), children (list), active, name, tag, layer, transform (position, localPosition, " +
            "eulerAngles, localScale, parent). Component members are the C# members (public + [SerializeField]); find them via " +
            "objects | get components | where $ is Light. Vectors/colors print as (x, y, z); write \"(1, 2, 3)\", [1, 2, 3] or " +
            "{x: 1, y: 2, z: 3}; colors also \"#ff8800\". Material shader properties: properties[\"_Color\"]. Destroyed objects read as " +
            "null (missing references: where target == null). Navigate with paths or get; select only projects output columns.\n" +
            "Results come back as compact rows (header line, then col|col). Long results are truncated — refine with where/first or use count. " +
            "Unknown members produce warnings with suggestions; rql-schema(type) lists a type's members.";

        public const string QueryExamplesExtra =
            "  console | where logType == \"Error\" | sort sequence desc | first 5 | select message\n" +
            "  scenes | select name path isDirty rootCount\n" +
            "  componentTypes | where name contains \"Collider\" | select name\n" +
            "  editor | select isPlaying isPaused selection";

        public const string QueryDescription =
            "Run a read-only RQL query against the live Unity Editor scene and return compact rows. One precise query beats many " +
            "exploratory calls. Changes need rql-mutate.\n\n" + Language + "\n\nExamples:\n" +
            "  objects | where name contains \"Enemy\" and active | select $ref name transform.position\n" +
            "  objects | get components | where $ is Light and intensity > 2 | select gameObject.name intensity color\n" +
            "  roots | recurse children | where components.count() == 1 | select $path name\n" +
            "  unity.assets(\"t:Material\") | select name shader.name properties[\"_Color\"]\n" + QueryExamplesExtra;

        public const string MutateStages =
            "  set path = expr, ...   assign members (paths may go through structs: set transform.position.y = transform.position.y + 1)\n" +
            "  call Method(args)      invoke a method the tools expose\n";

        public const string MutateExamples =
            "Examples:\n" +
            "  objects | where name == \"Player\" | set transform.position = \"(0, 1, 0)\"\n" +
            "  objects | get components | where $ is Light | set intensity = intensity * 2, color = \"#ffcc88\"\n" +
            "  objects | where tag == \"Enemy\" | set active = false\n" +
            "  unity.asset(\"Assets/Mats/Red.mat\") | set properties[\"_Color\"] = \"(1, 0, 0, 1)\"\n" +
            "  objects | where name == \"Spawner\" | set targets = append(targets, ref(\"GameObject#1234\"))\n" +
            "Create / add / remove (call; the receipt's result column holds the new object's $ref for follow-up queries):\n" +
            "  editor | call CreateGameObject(\"Props\")                                   new root GameObject\n" +
            "  editor | call CreateGameObject(\"Crate\", ref(\"GameObject#1234\"), \"Cube\")    child of a parent, optional primitive\n" +
            "  objects | where name == \"Crate\" | call AddComponent(\"Rigidbody\")           add a component by type name\n" +
            "  objects | where name == \"Old\" | call Destroy()                             remove a GameObject (components: get components | where $ is X | call Destroy())\n" +
            "  objects | where name == \"Lamp\" | set transform.parent = ref(\"GameObject#1234\")   reparent (keeps the world position)\n" +
            "  objects | where name == \"Crate\" | call Duplicate()                          copy next to it (returns the copy)\n" +
            "Scenes, prefabs, assets, Editor state:\n" +
            "  editor | call OpenScene(\"Assets/Scenes/Level.unity\")          editor | call NewScene(\"Assets/Scenes/New.unity\")\n" +
            "  scenes | where path == \"\" | call SaveAs(\"Assets/Scenes/New.unity\")   scenes | where name == \"Level\" | call SetActive()\n" +
            "  editor | call InstantiatePrefab(\"Assets/Prefabs/Enemy.prefab\", ref(\"GameObject#1234\"), \"(0, 0, 5)\")\n" +
            "  objects | where name == \"Crate\" | call SaveAsPrefab(\"Assets/Prefabs/Crate.prefab\")\n" +
            "  editor | call OpenPrefab(\"Assets/Prefabs/Enemy.prefab\")  then  prefab | recurse children | …  then  editor | call ClosePrefab()\n" +
            "  editor | call CreateMaterial(\"Assets/Mats/Glow.mat\")        editor | call MoveAsset(\"Assets/A.mat\", \"Assets/Mats/A.mat\")\n" +
            "  editor | set selection = [ref(\"GameObject#1234\")]         editor | set isPlaying = true\n" +
            "  profiler | call Start()      profiler | call Save(\"Logs/profile.json\")      (read: profiler | select memory frame)\n";

        public const string MutateDescription =
            "Change the live Unity Editor scene with an RQL query whose last stage is set or call. All-or-nothing: every target and " +
            "value is checked before anything is written; a failure changes nothing. Undoable in the Editor (Ctrl+Z). Changed assets, " +
            "scenes and the open prefab are saved to disk automatically; an untitled scene needs SaveAs(path). " +
            "dry_run=true returns the receipt ($ref, member, old, new) without changing anything. Do exactly the change requested; " +
            "a read-only member fails with readonly-member instead of being approximated.\n" +
            MutateStages + MutateExamples +
            "Object literals build values: set settings = {speed: 2, \"$type\": \"FastSettings\"}. At most 20 targets per call.\n\n" +
            QuerySyntaxShort;

        /// <summary>
        /// rql-mutate's reminder of the query part; the full language guide is in rql-query's description (sent once per
        /// request instead of twice).
        /// </summary>
        public const string QuerySyntaxShort =
            "Query part: the RQL of rql-query (full guide and the sources in its description); unity.assets(\"t:Material\"), " +
            "unity.asset(path), unity.find(\"Name\"), ref(\"Light#1234\"). Stages: where, select, sort, first N, get <path>, recurse children. Operators: == != < > and or " +
            "not, contains, starts-with, $ is Light. Paths a.b.c, list[0], map[\"key\"]. Vectors/colors as \"(1, 2, 3)\" or " +
            "\"#ff8800\"; enums as strings.";

        public const string SchemaDescription =
            "Without a type: RQL sources, asset functions, and the component types present in the loaded scenes (with counts), plus " +
            "GameObject and Transform members. With a type name (e.g. \"Light\", \"Enemy\", \"Vector3\"): that type's members " +
            "(name type; ro = read-only) and methods. Use it before querying an unfamiliar component.";
    }
}
