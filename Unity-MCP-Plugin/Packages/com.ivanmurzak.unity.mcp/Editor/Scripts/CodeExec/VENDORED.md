# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim, below a 4-line license header, from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec)
release **0.4.0** (tag `nuget-v0.4.0`, commit `58ef3ed`), published on NuGet as `com.IvanMurzak.CodeExec` 0.4.0. Its
C# sources are identical to `main` at `54d527b` (execution guards, Code-Exec PRs #7 to #11: snippet threads and tasks stop
with their run, `using var` as a first statement, the `MaxTimeoutMs` host cap, blocking waits stop at the budget, an
overrun is a Timeout, and a sliced wait still runs a queued task inline).

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
880027ea76fb25cbcb8c8abeb9f0aaf095e845628e6a7cd1e048069dfcf5de25 *Core/CodeRunner.cs
220254baf59d469ea1c26be03a59bbd644593673c62a52dccb19553272f2808c *Core/Compilation.cs
a58556a9f8871691ddab93089cfe63942f7f9c68619047735f0214dfe3988369 *Core/Contracts.cs
ac42954288bf128b16fa697e70d45b81f10f7e22139a0d819deb8434b2cb0319 *Core/ExecutionGuard.cs
985430e145e58227990d8f88bc451a4f806d3d72a4e1b76291b481839f67076c *Core/Hosting.cs
eb6777505f7afe05cc051ecd32d97d62296ebc795dcbe611a400acff8cd4d116 *Core/Limits.cs
22a082265c5180b2d2bca913efbe7580df74f27d7cf41ba4d0d19b513c084af2 *Core/References.cs
3169f7daa632f51c4990d189e540f229a592db1873d87a4b442c8bf414e1c7e0 *Core/Support.cs
0580c78fd9b9d82650ab8ac6e8971efcb43cfd69618da5780f735b97fc7351ad *Roslyn/GuardWriter.cs
c569f7e99ceacf8d4795a569d2b6de268cabf0c287ab5d1cb92113508b788052 *Roslyn/RoslynCSharpCompiler.cs
79e1cf267745d92e087affa1d435b50b00b693d0e9927f973bfe80913d393d63 *Roslyn/SnippetWriter.cs
```
