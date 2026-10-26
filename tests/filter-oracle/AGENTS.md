# tests/filter-oracle/

Oracle for the `tests-run` filter check: for every scenario Unity really runs the tool's own `Filter`, and the
tests it reports must equal `tests-run`'s count (`TestFilterMatch`). CI: `.github/workflows/filter_oracle.yml`,
only on PRs touching the counting code, this folder or a Unity / test-framework version.

- `Editor/` — the runner and its scenarios (`FilterOracle.cs`); a new filter shape is one line in `Scenarios`.
- `EditModeFixtures/`, `PlayModeFixtures/` — the tests the scenarios select; a new rule needs a fixture here.
- `.meta` files are Unity-generated: copy them back from the project after adding a file, never hand-write them.

Run locally (Windows paths as an example; exit 0 = all match, 1 = mismatch, 2 = no result):

```bash
cp -r tests/filter-oracle/{Editor,EditModeFixtures,PlayModeFixtures} Unity-Tests/2022.3.62f3/Assets/FilterOracle/
"C:/UnityEditor/2022.3.62f3/Editor/Unity.exe" -batchmode -nographics -projectPath Unity-Tests/2022.3.62f3 \
  -executeMethod com.IvanMurzak.Unity.MCP.FilterOracle.FilterOracle.Run -filterOracleOut filter-oracle.md -logFile oracle.log
```

`Assets/FilterOracle` is gitignored. The run temporarily disables the Play Mode domain reload and restores it.
