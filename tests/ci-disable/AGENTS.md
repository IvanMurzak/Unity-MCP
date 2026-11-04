# tests/ci-disable/

- `Editor/` — standalone Unity probe with no compile-time reference to MCP.
- Run in a prepared shared Unity project; it checks normal, disabled and excluded launches.
- `CiDisableProbe.Run` exits Unity with a failure code on missing evidence.
- Unity generates metadata; copy generated `.meta` files back from the test project.
