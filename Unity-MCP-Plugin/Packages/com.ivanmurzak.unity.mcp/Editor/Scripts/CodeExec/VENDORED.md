# Vendored: Code-Exec (C#)

`Core/` and `Roslyn/` are copied verbatim from [IvanMurzak/Code-Exec](https://github.com/IvanMurzak/Code-Exec) release
**0.2.0** + main `cd0bfdd` (run-time values, loading fixes): `dotnet/CodeExec/*.cs` and `dotnet/CodeExec.Roslyn/src/*.cs`. Source, not the NuGet DLL:
the adapter compiles against the Roslyn 4.8 this package already installs, and no NuGet pin / DLL changes are needed in
the test projects. Do not edit them here: change Code-Exec, then copy the files again and update this note and the
hashes below (`sha256sum Core/*.cs Roslyn/*.cs`).

License: PolyForm Shield 1.0.0 — https://polyformproject.org/licenses/shield/1.0.0
Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)

```
78c4454e4159037ad0e56c75d11bba5061222a8a05bbffff481f50724f3499e6 *Core/CodeRunner.cs
14a1c2ce6e0873887530b59dabee5c72908a72e025e96ae7c619041abb47d1b4 *Core/Compilation.cs
778f381514649e1922d45261b14f69f2214f62546e8e9c1f7d55c3faa21c0684 *Core/Contracts.cs
c6eb8149c0243d4cb6a40695e72611e3a82ad50bd0b4fb19eeb85e13af206502 *Core/Hosting.cs
e02bff170d8be201367ef8ac99634230bbad2b01bbe56e0c2aa54b5a6cdf5df2 *Core/References.cs
55db43d6d78c71f08ab774801b9660ac8c8a42ff0f71377829aa8bb85f904f16 *Core/Support.cs
7af85c7c56a53a1e2d1745832b5642fe2bdec9d2cb9cbfed890b1bf8860fdd8d *Roslyn/RoslynCSharpCompiler.cs
f556237f180ab2bd4ed750518bd05ed3474635f023f0bc5482f2b8cc662d3858 *Roslyn/SnippetWriter.cs
```
