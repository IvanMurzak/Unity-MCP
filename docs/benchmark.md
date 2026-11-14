<b>English</b> | [中文](benchmark.zh-CN.md) | [日本語](benchmark.ja.md) | [Español](benchmark.es.md) | [فارسی](benchmark.fa.md) | [हिन्दी](benchmark.hi.md) | [العربية](benchmark.ar.md)

# Benchmark: compact tools in the Unity Editor

Back to the [README](https://github.com/IvanMurzak/Unity-MCP#readme).

AI Game Developer gives the agent a few compact tools built on **RQL**, a query language over the Editor's data, instead of one tool per action. We ran the same tasks in the real Unity Editor with both tool catalogs. The agent solved the same number of tasks with **about 5× fewer prompt tokens**, fewer turns and fewer tool calls, and finished each task faster. A second, larger test on a sandbox game world shows the same pattern.

## Key numbers (Unity Editor)

| 5.3× | −31% | −39% | 59 / 60 |
| :---: | :---: | :---: | :---: |
| fewer prompt tokens per task | tool calls per task | time per task | tasks solved, in both catalogs |

<p align="center"><img src="img/benchmark/unity-editor.svg" alt="Unity Editor benchmark: prompt tokens per task 138,272 with per-action tools vs 26,013 with RQL tools; tool calls per task 5.5 vs 3.8; tasks solved 59 of 60 in both" width="100%"></p>

## Unity Editor results

| Metric | Per-action tools | RQL tools |
| --- | ---: | ---: |
| Tasks solved (task check passed, no unintended change) | 59 / 60 | 59 / 60 |
| Prompt tokens per task | 138,272 | **26,013** |
| Completion tokens per task | 673 | **306** |
| Model turns per task | 5.4 | **4.1** |
| Tool calls / errors per task | 5.5 / 0.5 | **3.8 / 0.2** |
| Tool result size per task (characters) | 6,273 | **657** |
| Tool definitions sent with every request (characters) | 93,194 | **22,439** |
| Wall time per task | 8.8 s | **5.4 s** |
| Tool execution time per task | 169 ms | **42 ms** |
| Cost per task | 1× | **about ⅓** |

**Why it is cheaper.** Most of the gap comes from two places: the tool definitions the agent receives with every request are 4.2× smaller, and tool results are 9.5× smaller. For bulk and query tasks the per-action catalog fell back to writing C# (`script-execute` 68 times across all runs, against 7 with RQL); with RQL the agent writes one query instead.

**Control run.** After the extension migrations and core fixes (undo of owner chains, asset paths, `unity.find`, number formatting), the RQL catalog solved **60 / 60** tasks with 23,274 prompt tokens, 3.4 tool calls and 4.8 s per task.

## Sandbox game world results

The RQL project measures the language on its own, outside Unity: a generated game world and an agent that either uses RQL or plain reflection tools (`find_objects`, `get_object` as a JSON dump, `describe_type`, `set_value`, `call_method`). The cleanest check used 30 new tasks written after all tuning was frozen, run once, 3 times each.

<p align="center"><img src="img/benchmark/sandbox.svg" alt="Sandbox benchmark: tokens per task 169,190 with plain reflection tools vs 7,817 with RQL; tool calls per task 36.9 vs 1.9; tasks solved 66.7% vs 100%" width="100%"></p>

| Run | Tasks solved | Tokens per task | Tool calls per task | Cost per success |
| --- | ---: | ---: | ---: | ---: |
| Reflection tools, tasks written after tuning | 66.7% | 169,190 | 36.9 | 40× |
| RQL, tasks written after tuning | **100%** | **7,817** | **1.9** | **1×** |
| Reflection tools, holdout tasks | 91.4% | 125,566 | 34.0 | 22× |
| RQL, holdout tasks | **100%** | **7,444** | **1.9** | **1×** |

Reflection tools fail mostly by running out of steps on "search everything" tasks, reading the world one object at a time.

## Methodology

**Unity Editor.** Measured on Unity-MCP 0.93.4 (every enabled tool, one tool per action: `gameobject-*`, `assets-*`, `scene-*`, `object-*`, `console-get-logs`, `script-execute`, `tests-run`, …) vs the RQL catalog (`rql-query`, `rql-mutate`, `rql-schema`, `script-execute`, `tests-run`, `screenshot-isolated`, `unity-tool-list`). Same harness, scene and model for both: `deepseek-flash`, reasoning off, temperature 0. 20 tasks × 3 runs, Unity 6000.6.4f1. Each condition got exactly the tools an MCP client sees, with their own names, descriptions and input schemas, called through the plugin. A task passes only when its check passes and nothing else in the scene changed.

**Sandbox.** Same tasks, grader, model and budgets for both tool surfaces; only the tools differ. Model DeepSeek V4.1-Flash, at most 12 steps per task, 3 runs per task. A hidden grader computes the expected answer independently; mutation tasks fail on any unintended change. Holdout tasks were never used for tuning; the second task set was written after tuning was frozen and run once.

## Caveats

- **One model family.** Both tests used DeepSeek models. Other models may show different numbers.
- **Versions measured.** The Unity Editor numbers compare Unity-MCP 0.93.4 with the RQL catalog on one scene and one Unity version; tools and models change over time.
- **Small samples.** 60 runs per condition in the Editor; 81 to 90 runs per condition in the sandbox results above.
- **Same author.** The RQL tasks and sandbox were designed by the author of the language. Holdout tasks and the task set written after tuning reduce this bias but do not remove it.
- **Cost is a ratio.** Prompt caching makes cost depend partly on other traffic; token counts and tool calls are the more stable signals.

## Raw data

- Unity Editor report: [`evaluation/reports/unity-mcp-old-vs-rql-branch.md`](https://github.com/IvanMurzak/RQL/blob/main/evaluation/reports/unity-mcp-old-vs-rql-branch.md) in the RQL repository.
- Sandbox results: [RQL `RESULTS.md`](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md), sections 3.3 and 3.4, and the [evaluation methodology](https://github.com/IvanMurzak/RQL/blob/main/docs/evaluation-methodology.md).

Both measurements come from the [RQL](https://github.com/IvanMurzak/RQL) project's evaluation harness.
