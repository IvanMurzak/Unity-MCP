# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec) `main` at
commit `2b784e4` (execution guards, Code-Exec PRs #7 and #8), which is **not released yet**: release **0.3.2-dev.2b784e4**, a
pre-release on top of 0.3.1 (tag `nuget-v0.3.1`, commit `96c2919`). The release guard
(`commands/release_dependency_guard.py`) refuses this marker on purpose: before a Unity-MCP release, copy the files again
from the published Code-Exec release and record that version here.

Files: `dotnet/CodeExec/*.cs` and `dotnet/CodeExec.Roslyn/src/*.cs`. Source, not the NuGet DLL:
the adapter compiles against the Roslyn 4.8 this package already installs, and no NuGet pin / DLL changes are needed in
the test projects. Do not edit them here: change Code-Exec, then copy the files again and update this note and the
hashes below (`sha256sum Core/*.cs Roslyn/*.cs`).

License: PolyForm Shield 1.0.0 — https://polyformproject.org/licenses/shield/1.0.0
Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)

```
b66fc569401fbe1491beb10b4814f8ef2e0a633d8731914859ff5e4eedb1ffba *Core/CodeRunner.cs
220254baf59d469ea1c26be03a59bbd644593673c62a52dccb19553272f2808c *Core/Compilation.cs
9c689deafcc1534c2bd9f170f266355cbe1f3d36427ca1c8bccbbcde43664593 *Core/Contracts.cs
37e21345ffb53b7e7c70500c54ba0b9e7f3eef59e39f50832259e8f4a14ec228 *Core/ExecutionGuard.cs
985430e145e58227990d8f88bc451a4f806d3d72a4e1b76291b481839f67076c *Core/Hosting.cs
eb6777505f7afe05cc051ecd32d97d62296ebc795dcbe611a400acff8cd4d116 *Core/Limits.cs
22a082265c5180b2d2bca913efbe7580df74f27d7cf41ba4d0d19b513c084af2 *Core/References.cs
3169f7daa632f51c4990d189e540f229a592db1873d87a4b442c8bf414e1c7e0 *Core/Support.cs
8d1e5d913c1a4d4751e67c61929aa11658243c560eaac05541f0c7d98738d403 *Roslyn/GuardWriter.cs
c569f7e99ceacf8d4795a569d2b6de268cabf0c287ab5d1cb92113508b788052 *Roslyn/RoslynCSharpCompiler.cs
cda17a995db83cd7dd61cfc978fb587f2cc862e56f461d0aefd32865bdfb610d *Roslyn/SnippetWriter.cs
```
