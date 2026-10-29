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
using Rql.Execution;

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
            "editor (Editor state and operations), settings (Project Settings: physics, time, quality, player, build). " +
            "Assets: unity.assets(\"t:Material\") or unity.assets(\"t:Prefab\", \"Assets/Enemies\"), unity.asset(\"Assets/Mats/Red.mat\"). " +
            "ref(\"Light#1234\") resolves a $ref; unity.find(\"Player\") or unity.find(\"Level/Pillar_1\") a GameObject by unique name or path.\n" +
            "Stages:\n" +
            "  where <condition>                  filter (a boolean: where sharedMaterial != null, not where sharedMaterial)\n" +
            "  select a b.c alias=(expr)          project columns (wrap computed expressions in parentheses)\n" +
            "  sort <expr> [desc], ...            stable sort (computed keys in parentheses: sort (a * b) desc)\n" +
            "  first N | skip N | count\n" +
            "  get <path>                         replace each item by a member (lists are flattened)\n" +
            "  recurse children [max-depth=N] [include-self=false]   walk the hierarchy (input items included by default)\n" +
            "  distinct [expr] | sum expr | min expr | max expr | avg expr   (distinct keeps whole items; values: get tag | distinct)\n" +
            "  group-by expr                      rows key|count; group-by a, b (or kind=$type) gives one column per key, then count;\n" +
            "                                     items = the group's items: … | where $ is Light | group-by type | select key count avg=(items.avg(intensity))\n" +
            "Expressions: == != < <= > >= and or not + - * / %; contains, starts-with, ends-with (case-insensitive); " +
            "matches \"regex\"; x in [a, b]; x is null; x is not null; $ is Light (includes subclasses); has(member); " +
            "c ? a : b or if(c, a, b) (only the taken branch runs; where a stage takes one expression, wrap it: select kind=($ is Light ? \"light\" : \"other\")). " +
            "Literals: numbers, \"strings\", true/false/null, lists [a, b], objects {x: 1, y: 2}. Text functions: lower upper len, " +
            "replace(s, from, to) (case-insensitive), replace-regex(s, pattern, repl) ($1 = group 1), substring(s, start[, length]), " +
            "split(s, sep), trim(s), pad(v, width[, fill]) — also as methods: name.replace(\"Old\", \"New\"), name.substring(0, 4).\n" +
            "Paths: a.b.c, list[0], map[\"key\"]; .member on a list maps over its elements. List methods: .any(pred) .all(pred) " +
            ".count([pred]) .sum(e) .avg(e) .min(e) .max(e) .first([pred]); inside them bare names refer to the element, falling back to the outer item.\n" +
            "Meta: $ (current item), $ref (stable id such as Light#1234), $type, $path (the item's place in this query, e.g. roots[0].children[2]), $index, $depth. Enums compare to strings (type == \"Spot\", or the Inspector's name: shadowCastingMode == \"Two Sided\"); " +
            "== on strings is case-sensitive.\n" +
            "Unity: GameObject has components (list), children (list), active, name, path (hierarchy path \"Level/Props/Crate\"), tag, " +
            "layer, layerName, transform (position, localPosition, " +
            "eulerAngles, localScale, parent; call LookAt(target) turns it toward a GameObject or a position). Component members are the C# members (public + [SerializeField]); find them via " +
            "objects | get components | where $ is Light. Vectors/colors print as (x, y, z); write \"(1, 2, 3)\", [1, 2, 3] or " +
            "{x: 1, y: 2, z: 3}; colors also \"#ff8800\". An asset's file: assetPath; its import settings: get importer. Material shader properties: " +
            "properties[\"_Color\"] (the main color / texture: color, mainTexture); a Material Variant has a parent. A Mesh value also takes a primitive's name (\"Cylinder\"), a Material value a built-in material's (\"Default-Material\"). Prefab instances: " +
            "prefabSource, prefabOverrides (all of the instance's on its root, an object's own on a part). Destroyed objects read as " +
            "null; a GameObject's missingReferences lists references to deleted assets or objects (where missingReferences.count() > 0). Navigate with paths or get; select only projects output columns.\n" +
            "Results come back as compact rows (header line, then col|col). Long results are truncated — refine with where/first or use count. " +
            "Unknown members produce warnings with suggestions; rql-schema(type) lists a type's members.\n" +
            LanguageRules;

        /// <summary>
        /// The rules every RQL host shares (conditions, ?:, list equality, group items, receipts, call, bulk create, vectors and
        /// vector functions, missing map entries, typed literals), with Unity's creation call in the examples:
        /// <c>RqlLanguageGuide.RulesFor("editor.CreateGameObject")</c> of the bundled runtime. A copy only because tool
        /// descriptions are attribute constants; RqlGuideTests fails when it differs from the runtime's text.
        /// </summary>
        public const string LanguageRules =
            "Language rules:\n" +
            "- Conditions are booleans. where, and / or / not, ?: and the predicates of .any() .all() .count() .first() take a comparison or has(), never a bare value: where material != null (or has(material)), where count > 0, where name != \"\", where len(tags) > 0. A number, string, object or list there fails with type-mismatch; null counts as false.\n" +
            "- cond ? a : b (the same as if(cond, a, b)) picks a value and evaluates only the taken branch: set y = (y < 0 ? 0 : y). Inside select, wrap computed values in parentheses: select kind = (intensity > 2 ? \"bright\" : \"dim\").\n" +
            "- Lists compare by value, in order: tags == [\"a\", \"b\"], x in [[1, 2], [3, 4]]; distinct and group-by merge equal lists.\n" +
            "- group-by gives rows with key (or one column per key), count and the group's items: group-by kind | select key, count, avg = items.avg(weight); also items.sum(e), items.min(e), items.max(e), items.count(pred), items.first().name. Group by the item's type with $type (group-by $type); a bare type is a member that most items do not have.\n" +
            "- Receipts list only real changes: a set row whose value stayed the same is left out and counted as unchanged, so no rows with N unchanged means the values already matched.\n" +
            "- call Method(args) runs the method of each item (the items must be objects). call source.Method(args) runs the method of that one source object once per stream item, with $, $index, $path and $depth in the arguments being the item's; every call counts against the target limit, and every argument is checked before the first call (a dry run checks without calling).\n" +
            "- Bulk create: range(start, end[, step]) streams numbers (end excluded), and a list literal streams its elements, so one call creates many objects: range(0, 300) | call editor.CreateGameObject(\"Cube\", {position: [($ % 15) * 2, 0, floor($ / 15) * 2]}) builds a 15-wide grid, [[\"A\", 1], [\"B\", 2]] | call editor.CreateGameObject($[0], $[1]) one object per entry. The receipt has one row per call with the $ref of the created object.\n" +
            "- Vectors and colors the host marks as component-wise compute component by component: set Scale = Scale * 1.5, Position + [0, 1, 0], Position - Offset, -Velocity, v / 2. Adding a number to a vector is a type-mismatch (add [x, y, z] instead); quaternions and other types without that mark stay type-mismatch. Lists of numbers are vectors too: [x, 0, z] * k, [1, 2] + [3, 4]. Vector functions work on both: length(v) (or v.length(), magnitude), normalize(v), distance(a, b), dot(a, b), lerp(a, b, t); len(x) stays the size of a string or list.\n" +
            "- A missing map entry reads as null: props[\"k\"] == null and props[\"k\"] is null are true without the entry, and has(props[\"k\"]) tests whether it exists. A misspelt member is not null: it stays unknown and is reported.\n" +
            "- Arguments by name: call Method({name: value, ...}) with one object literal that no overload takes as a whole binds each key to the parameter of that name; parameters left out keep their defaults, and an unknown name fails listing the parameters: range(0, 10) | call editor.CreateGameObject({name: \"Tile\" + $, position: [$ * 2, 0, 0]}). Engine objects (scene objects, nodes, actors) are never built from an object literal: pass an existing one, e.g. ref(\"Type#id\"). Value-like assets and owned subobjects that the host marks are built from a typed literal: set mesh = {$type: \"TypeName\", size: 2}.\n";

        public const string QueryExamplesExtra =
            "  console | where logType == \"Error\" | sort sequence desc | first 5 | select message\n" +
            "  scenes | select name path isDirty rootCount\n" +
            "  componentTypes | where name contains \"Collider\" | select name\n" +
            "  editor | select isPlaying isPaused selection\n" +
            "  settings | select physics.gravity time.fixedDeltaTime quality.levels";

        public const string QueryDescription =
            "Run a read-only RQL query against the live Unity Editor scene and return compact rows. One precise query beats many " +
            "exploratory calls. Changes need rql-mutate.\n\n" + Language + "\n\nExamples:\n" +
            "  objects | where name contains \"Enemy\" and active | select $ref name transform.position\n" +
            "  objects | get components | where $ is Light and intensity > 2 | select gameObject.name intensity color\n" +
            "  roots | recurse children | where components.count() == 1 | select $path name\n" +
            "  unity.assets(\"t:Material\") | select name shader.name properties[\"_Color\"]\n" + QueryExamplesExtra;

        public const string MutateStages =
            "  set path = expr, ...   assign members (paths may go through structs: set transform.position.y = transform.position.y + 1)\n" +
            "                         values see each target: $ and $index (its position in the stream entering set / call)\n" +
            "  call Method(args)      invoke a method the tools expose\n";

        public const string MutateExamples =
            "Examples:\n" +
            "  objects | where name == \"Player\" | set transform.position = \"(0, 1, 0)\"\n" +
            "  objects | get components | where $ is Light | set intensity = intensity * 2, color = \"#ffcc88\"\n" +
            "  objects | where tag == \"Enemy\" | set active = false\n" +
            "  objects | where name starts-with \"Pillar_\" | set name = name.replace(\"Pillar_\", \"Column_\")   bulk rename\n" +
            "  objects | where name starts-with \"Crate\" | sort name | set transform.position.x = $index * 2   spread along x\n" +
            "  objects | where name starts-with \"Coin\" | set transform.localScale = transform.localScale * 1.5   vectors / colors compute per component (a color's alpha too)\n" +
            "  objects | where name starts-with \"Lamp\" | set transform.position = transform.position + [0, 2.5, 0]\n" +
            "  objects | where name starts-with \"Rock\" | set transform.position = [transform.position.x, 0, transform.position.z] * 2   lists of numbers are vectors\n" +
            "  objects | where name starts-with \"Enemy\" and distance(transform.position, unity.find(\"Player\").transform.position) < 10 | set active = false   (units: meters)\n" +
            "  objects | where name == \"Drone\" | set transform.position = transform.position + normalize(transform.forward) * 3   also length(v), dot(a, b), lerp(a, b, t)\n" +
            "  unity.asset(\"Assets/Mats/Red.mat\") | get properties | where Key ends-with \"Color\" | set Value = \"#ffffff\"   map entries write through\n" +
            "  unity.asset(\"Assets/Mats/Red.mat\") | set properties[\"_Color\"] = \"(1, 0, 0, 1)\"\n" +
            "  unity.asset(\"Assets/Mats/Red.mat\") | set parent = unity.asset(\"Assets/Mats/Base.mat\")   make it a Material Variant of Base\n" +
            "  unity.asset(\"Assets/Data/Items.asset\") | get rows | where Category == \"Tool\" | set Price = round(Price * 1.15)   data asset (ScriptableObject list rows)\n" +
            "  objects | where name starts-with \"Pillar\" | get components | where $ is MeshFilter | set sharedMesh = \"Cylinder\"   built-in mesh\n" +
            "  objects | get components | where $ is MeshRenderer and sharedMaterial == null | set sharedMaterial = \"Default-Material\"   built-in material\n" +
            "  objects | where name == \"Crate\" | get components | where $ is MeshRenderer | set sharedMaterial = {$type: \"Material\", color: \"#ff8800\"}   a new material (saved with the scene)\n" +
            "  objects | where name == \"Shop\" | get components | where $ is ShopStock | set item = {$type: \"ItemData\", price: 3}   a new data ScriptableObject\n" +
            "  unity.asset(\"Assets/Art/hero.png\") | get importer | set textureType = \"Sprite\", spriteImportMode = \"Single\"   import settings (reimported)\n" +
            "  objects | where name == \"Spawner\" | set targets = append(targets, ref(\"GameObject#1234\"))\n" +
            "Create / add / remove (call; the receipt's result column holds the new object's $ref for follow-up queries):\n" +
            "  editor | call CreateGameObject(\"Props\")                                   new root GameObject\n" +
            "  editor | call CreateGameObject(\"Crate\", ref(\"GameObject#1234\"), \"Cube\")    child of a parent, optional primitive\n" +
            "  range(0, 300) | call editor.CreateGameObject(\"Cube_\" + $, {primitive: \"Cube\", position: [($ % 15) * 2, 0, floor($ / 15) * 2]})   with max_targets=300:\n" +
            "                         bulk create, one call per item: a 15x20 grid in ONE request and ONE Undo step; the receipt lists the first 50 new objects' $refs (query the rest by name)\n" +
            "  [[\"Crate\", [0, 0, 0]], [\"Barrel\", [2, 0, 0]]] | call editor.CreateGameObject($[0], {primitive: \"Cube\", position: $[1]})   one per entry\n" +
            "                         {placement}: primitive, parent: ref(…), position / rotation (Euler degrees) / scale as [x, y, z], local to the parent\n" +
            "  editor | call CreateGameObject({name: \"Crate\", primitive: \"Cube\", position: [1, 0, 2], scale: [2, 2, 2]})   arguments by name: one literal, any order, the rest default\n" +
            "  objects | where name == \"Crate\" | call AddComponent(\"Rigidbody\")           add a component by type name\n" +
            "  objects | where name == \"Old\" | call Destroy()                             remove a GameObject (components: get components | where $ is X | call Destroy())\n" +
            "  objects | where name == \"Lamp\" | set transform.parent = ref(\"GameObject#1234\")   reparent (keeps the world position)\n" +
            "  objects | where name == \"Crate\" | call Duplicate()                          copy next to it (returns the copy)\n" +
            "  objects | where name == \"Main Camera\" | call LookAt(unity.find(\"Player\"))   aim at a GameObject (or a position \"(0, 1, 0)\")\n" +
            "Scenes, prefabs, assets, Editor state:\n" +
            "  editor | call OpenScene(\"Assets/Scenes/Level.unity\")          editor | call NewScene(\"Assets/Scenes/New.unity\")\n" +
            "  scenes | where path == \"\" | call SaveAs(\"Assets/Scenes/New.unity\")   scenes | where name == \"Level\" | call SetActive()\n" +
            "  editor | call InstantiatePrefab(\"Assets/Prefabs/Enemy.prefab\", {parent: ref(\"GameObject#1234\"), position: [0, 0, 5]})\n" +
            "  range(0, 10) | call editor.InstantiatePrefab(\"Assets/Prefabs/Coin.prefab\", {position: [$ * 2, 1, 0]})   ten instances, one Undo step\n" +
            "  objects | where name == \"Crate\" | call SaveAsPrefab(\"Assets/Prefabs/Crate.prefab\")   (on a prefab instance: a Prefab Variant)\n" +
            "  objects | where name == \"Coin_3\" | call RevertPrefabOverrides()   or ApplyPrefabOverrides() (read: select prefabSource prefabOverrides)\n" +
            "  editor | call OpenPrefab(\"Assets/Prefabs/Enemy.prefab\")  then  prefab | recurse children | …  then  editor | call ClosePrefab()\n" +
            "  editor | call CreateMaterial(\"Assets/Mats/Glow.mat\")        editor | call MoveAsset(\"Assets/A.mat\", \"Assets/Mats/A.mat\")\n" +
            "  editor | set selection = [ref(\"GameObject#1234\")]         editor | set isPlaying = true   (scripts reload: retry a call that fails right after)\n" +
            "  editor | call AddLayer(\"Pickups\")  then  objects | where name starts-with \"Pickup\" | set layerName = \"Pickups\"   (read: editor | select layers tags)\n" +
            "  editor | call AddTag(\"Interactive\")  then  objects | where name starts-with \"Barrel\" | set tag = \"Interactive\"\n" +
            "  profiler | call Start()      profiler | call Save(\"Logs/profile.json\")      (read: profiler | select memory frame)\n" +
            "  settings | set physics.gravity = \"(0, -12, 0)\", time.fixedDeltaTime = 0.01   Project Settings (saved, undoable)\n" +
            "  settings | set build.scenes = append(build.scenes, \"Assets/Scenes/Level2.unity\")   scenes in the build\n";

        /// <summary>How an agent changes things safely and transparently (wave 6 findings).</summary>
        public const string AgentRules =
            "Agent rules: delete or destroy in two steps: first list what matches (rql-query, or dry_run=true) and show it, then remove " +
            "only that in a second call. When you map a name the user misspelt to a real one (an object, component or member), say so. " +
            "Unity units are meters, degrees (rotation) and seconds.\n";

        public const string MutateDescription =
            "Change the live Unity Editor scene with an RQL query whose last stage is set or call. All-or-nothing: every target and " +
            "value is checked before anything is written; a failure changes nothing. Undoable: one Undo step per call (Ctrl+Z; from an " +
            "agent: script-execute UnityEditor.Undo.PerformUndo(), then editor | call SaveScenes() and editor | call SaveAssets()). Changed assets, " +
            "scenes and the open prefab are saved to disk automatically; an untitled scene needs SaveAs(path). " +
            "dry_run=true returns the receipt ($ref, member, old, new) without changing anything. Do exactly the change requested; " +
            "a read-only member fails with readonly-member instead of being approximated.\n" +
            MutateStages + MutateExamples +
            "Object literals build values: set settings = {speed: 2, $type: \"FastSettings\"}, and so do Materials and data ScriptableObjects " +
            "(into a scene object; an asset cannot hold a new one: create the asset and pass its path). GameObjects, components and other " +
            "assets are never built from a literal: pass unity.find(\"Name\"), ref(\"Type#id\") or an asset path. At most 200 targets (or bulk-create items) per call; " +
            "max_targets raises the limit (up to 1000) for an intended bulk change. The receipt lists the assignments that change a " +
            "value; \"N unchanged\" counts the ones whose target already held it (written anyway). A misspelt member that empties the stream of set / call " +
            "fails with unknown-member (nothing changed); a real member that no matched item has is a no-op.\n" +
            AgentRules + "\n" +
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
            "Without a type: RQL sources, asset functions, the installed extensions (always active) with their guide topics, and the " +
            "component types present in the loaded scenes (with counts), plus GameObject and Transform members. \"guide:<topic>\" prints one of the " +
            "listed guides. With a type name (e.g. \"Light\", \"Enemy\", \"Vector3\"): that type's members " +
            "(name type; ro = read-only) and methods. Use it before querying an unfamiliar component.";
    }
}
