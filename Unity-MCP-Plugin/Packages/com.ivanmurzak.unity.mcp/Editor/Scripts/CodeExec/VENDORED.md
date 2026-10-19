# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec) release
**0.2.1** (commit `79f5281`): `dotnet/CodeExec/*.cs` and `dotnet/CodeExec.Roslyn/src/*.cs`. Source, not the NuGet DLL:
the adapter compiles against the Roslyn 4.8 this package already installs, and no NuGet pin / DLL changes are needed in
the test projects. Do not edit them here: change Code-Exec, then copy the files again and update this note and the
hashes below (`sha256sum Core/*.cs Roslyn/*.cs`).

License: PolyForm Shield 1.0.0 — https://polyformproject.org/licenses/shield/1.0.0
Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)

```
78c4454e4159037ad0e56c75d11bba5061222a8a05bbffff481f50724f3499e6 *Core/CodeRunner.cs
14a1c2ce6e0873887530b59dabee5c72908a72e025e96ae7c619041abb47d1b4 *Core/Compilation.cs
778f381514649e1922d45261b14f69f2214f62546e8e9c1f7d55c3faa21c0684 *Core/Contracts.cs
003483d3135cc3d916443a24fdfbe2b63ef8b575add395364d41694fb9aa2d45 *Core/Hosting.cs
26e8b9136592a6a0b82b0800475f3ef865b3c163bee274f9bac9ffbd8722e840 *Core/References.cs
941957969f809a4b1deaa1d600daf1861202f667819334b692e9f137bdc23ebe *Core/Support.cs
7af85c7c56a53a1e2d1745832b5642fe2bdec9d2cb9cbfed890b1bf8860fdd8d *Roslyn/RoslynCSharpCompiler.cs
f556237f180ab2bd4ed750518bd05ed3474635f023f0bc5482f2b8cc662d3858 *Roslyn/SnippetWriter.cs
```
