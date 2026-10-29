# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim, below a 4-line license header, from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec) `main` at
commit `2556a2f` (execution guards, Code-Exec PRs #7, #8 and #9: snippet threads and tasks stop with their run, `using var`
as a first statement, the `MaxTimeoutMs` host cap), which is **not released yet**: release **0.3.2-dev.2556a2f**, a
pre-release on top of 0.3.1 (tag `nuget-v0.3.1`, commit `96c2919`). The release guard
(`commands/release_dependency_guard.py`) refuses this marker on purpose: before a Unity-MCP release, copy the files again
from the published Code-Exec release and record that version here.

Files: `dotnet/CodeExec/*.cs` and `dotnet/CodeExec.Roslyn/src/*.cs`. Source, not the NuGet DLL:
the adapter compiles against the Roslyn 4.8 this package already installs, and no NuGet pin / DLL changes are needed in
the test projects. Do not edit them here: change Code-Exec, then copy the files again, put the same header (with the new
commit) on top of each, and update this note, `THIRD-PARTY-NOTICES.md` and the hashes below. The hashes are of the
upstream files, i.e. each vendored file without its first 4 lines (`for f in Core/*.cs Roslyn/*.cs; do printf '%s *%s\n'
"$(tail -n +5 "$f" | sha256sum | cut -d' ' -f1)" "$f"; done`).

License: PolyForm Shield 1.0.0 — https://polyformproject.org/licenses/shield/1.0.0, not this package's Apache-2.0. The
terms and the notice are in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md); every vendored file names them in its header.
Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)

```
49ead59797d2157b79bba2f5eb4e423806493c1cbd597b05b9a49fa5d007d911 *Core/CodeRunner.cs
220254baf59d469ea1c26be03a59bbd644593673c62a52dccb19553272f2808c *Core/Compilation.cs
a58556a9f8871691ddab93089cfe63942f7f9c68619047735f0214dfe3988369 *Core/Contracts.cs
cbc9c9f102256e0e0b265e9cac71e8e44d8d7de2966fa5507779778607118398 *Core/ExecutionGuard.cs
985430e145e58227990d8f88bc451a4f806d3d72a4e1b76291b481839f67076c *Core/Hosting.cs
eb6777505f7afe05cc051ecd32d97d62296ebc795dcbe611a400acff8cd4d116 *Core/Limits.cs
22a082265c5180b2d2bca913efbe7580df74f27d7cf41ba4d0d19b513c084af2 *Core/References.cs
3169f7daa632f51c4990d189e540f229a592db1873d87a4b442c8bf414e1c7e0 *Core/Support.cs
7969275d8e4a0f979b596d38382c744b048d9fc010721929dd1be4e3da3882b3 *Roslyn/GuardWriter.cs
c569f7e99ceacf8d4795a569d2b6de268cabf0c287ab5d1cb92113508b788052 *Roslyn/RoslynCSharpCompiler.cs
79e1cf267745d92e087affa1d435b50b00b693d0e9927f973bfe80913d393d63 *Roslyn/SnippetWriter.cs
```
