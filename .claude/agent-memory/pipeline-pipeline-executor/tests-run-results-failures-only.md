---
name: tests-run-results-failures-only
description: unity-mcp tests-run EditMode response — Results[] lists only non-passing tests; trust Summary counts for the verdict
metadata:
  type: project
---

The `unity-mcp-cli run-tool tests-run` EditMode response returns `structured.result.Summary.{TotalTests, PassedTests, FailedTests, SkippedTests}` plus `structured.result.Results[]`. **`Results[]` contains ONLY the failing (non-passing) tests** — passing tests are counted in the Summary but NOT enumerated.

**Why:** During #791 work the response was consistently ~6.4 KB with 7 leaf entries, which looked like the runner had discovered only 7 of ~1000 tests (a feared "Bee stale cache" / "discovery cache" problem). It was a misread: `Summary.TotalTests` was 1001 with 994 passing; the 7 entries were just the failures. Clearing `Library/Bee` + `Library/ScriptAssemblies` and cold-restarting the Editor changed nothing because nothing was actually broken.

**How to apply:** When verifying the EditMode gate, parse `Summary.PassedTests` / `FailedTests` for the real count. Do NOT infer "tests weren't discovered" from a short `Results[]` — that list is failures-only by design. Also: the `testName` / `testClass` filter on `tests-run` is unreliable (it has run the full suite regardless of filter, or matched a loose subset), so prefer an unfiltered EditMode run and read the Summary.
