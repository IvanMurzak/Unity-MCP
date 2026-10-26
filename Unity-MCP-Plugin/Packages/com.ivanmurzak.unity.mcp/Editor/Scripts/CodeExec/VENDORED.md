# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec) release
**0.3.0** (tag `nuget-v0.3.0`, commit `6d9c0aa`): `dotnet/CodeExec/*.cs` and `dotnet/CodeExec.Roslyn/src/*.cs`. Source, not the NuGet DLL:
the adapter compiles against the Roslyn 4.8 this package already installs, and no NuGet pin / DLL changes are needed in
the test projects. Do not edit them here: change Code-Exec, then copy the files again and update this note and the
hashes below (`sha256sum Core/*.cs Roslyn/*.cs`).

License: PolyForm Shield 1.0.0 — https://polyformproject.org/licenses/shield/1.0.0
Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)

```
1c8166455ca798656f90e770d55967c2de339b7ce81437af0161ab2ef22f2f6b *Core/CodeRunner.cs
14a1c2ce6e0873887530b59dabee5c72908a72e025e96ae7c619041abb47d1b4 *Core/Compilation.cs
f303a9340b9b185203368279487f4e3d00a9c1a188ff1345ea13284fab1a0457 *Core/Contracts.cs
003483d3135cc3d916443a24fdfbe2b63ef8b575add395364d41694fb9aa2d45 *Core/Hosting.cs
eb6777505f7afe05cc051ecd32d97d62296ebc795dcbe611a400acff8cd4d116 *Core/Limits.cs
26e8b9136592a6a0b82b0800475f3ef865b3c163bee274f9bac9ffbd8722e840 *Core/References.cs
fdf36b7c7aa674ee96069faaf7e736d244842387d096920a32f0f3b8a778b69f *Core/Support.cs
c7d7e5c2a5f7012b86a318fba951d4a0bfcdf99d0ea550882e767d8fd92e792a *Roslyn/RoslynCSharpCompiler.cs
cda17a995db83cd7dd61cfc978fb587f2cc862e56f461d0aefd32865bdfb610d *Roslyn/SnippetWriter.cs
```
