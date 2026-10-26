# tests/filter-oracle/

Oracle for the `tests-run` filter check: for every scenario Unity really runs the tool's own `Filter`, and the
tests it reports must equal `tests-run`'s count (`TestFilterMatch`). CI: `.github/workflows/filter_oracle.yml`,
only on PRs touching the tests-run code, this folder or a Unity / test-framework version.

- `Editor/` — the runner and its scenarios (`FilterOracle.cs`); a new filter shape is one line in `Scenarios`.
- `EditModeFixtures/`, `PlayModeFixtures/` — the tests the scenarios select; a new rule needs a fixture here.
- `.meta` files are Unity-generated: copy them back from the project after adding a file, never hand-write them.

Exit codes: 0 every count matches, 3 a mismatch, 2 the oracle stopped early (a run, a teardown or the test tree
took too long; the report says where); Unity's own failures (a compile error) exit 1. The report goes to
`-filterOracleOut` (relative to the project folder; default `Logs/filter-oracle.md`).

Run locally (Windows paths as an example):

```bash
mkdir -p Unity-Tests/2022.3.62f3/Assets/FilterOracle
cp -r tests/filter-oracle/{Editor,EditModeFixtures,PlayModeFixtures} Unity-Tests/2022.3.62f3/Assets/FilterOracle/
"C:/UnityEditor/2022.3.62f3/Editor/Unity.exe" -batchmode -nographics -projectPath Unity-Tests/2022.3.62f3 \
  -executeMethod com.IvanMurzak.Unity.MCP.FilterOracle.FilterOracle.Run -logFile Unity-Tests/2022.3.62f3/Logs/oracle.log
```

`Assets/FilterOracle` is gitignored. The run disables the Play Mode domain reload and restores it.
