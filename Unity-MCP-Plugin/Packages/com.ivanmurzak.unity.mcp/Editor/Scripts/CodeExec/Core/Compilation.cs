#nullable enable
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>A source file handed to the compiler: an include, path relative to the include root.</summary>
    public sealed class SourceFile
    {
        public SourceFile(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }
        public string Text { get; }
    }

    /// <summary>A snippet parameter as source: <c>TypeName</c> is a fully qualified C# type (<c>global::...</c>).</summary>
    public sealed class ParameterDeclaration
    {
        public ParameterDeclaration(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }

        public string Name { get; }
        public string TypeName { get; }
    }

    /// <summary>What Core asks a compiler for. Free of Roslyn types on purpose.</summary>
    public sealed class CompileRequest
    {
        public string Code { get; set; } = "";
        public IReadOnlyList<string> Usings { get; set; } = Array.Empty<string>();
        public IReadOnlyList<SourceFile> Includes { get; set; } = Array.Empty<SourceFile>();
        public IReadOnlyList<string> ReferencePaths { get; set; } = Array.Empty<string>();
        public IReadOnlyList<ParameterDeclaration> Parameters { get; set; } = Array.Empty<ParameterDeclaration>();
        public string AssemblyName { get; set; } = "CodeExec.Snippet";

        /// <summary>Set = full-source mode: compile <see cref="Code"/> verbatim, invoke this type's method.</summary>
        public string? EntryType { get; set; }

        public string? EntryMethod { get; set; }
    }

    public sealed class CompileResult
    {
        public bool Success { get; set; }
        public byte[]? Image { get; set; }

        /// <summary>Portable PDB, when the compiler emitted one (line numbers in runtime stack traces).</summary>
        public byte[]? Symbols { get; set; }

        public string EntryType { get; set; } = "";
        public string EntryMethod { get; set; } = "";

        /// <summary>Static type of a snippet's result expression (<c>System.Int32</c>), when the compiler knows it; else null.</summary>
        public string? ResultType { get; set; }
        public IReadOnlyList<CodeDiagnostic> Diagnostics { get; set; } = Array.Empty<CodeDiagnostic>();

        /// <summary>Simple names the compiler could not resolve (CS0246 / CS0103): the input for auto-import.</summary>
        public IReadOnlyList<string> UnresolvedNames { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// The only seam to Roslyn. The implementation ships as SOURCE (package com.IvanMurzak.CodeExec.Roslyn) and is
    /// compiled by the consumer against whichever Microsoft.CodeAnalysis.CSharp version the consumer chose.
    /// Must be safe to call from a background thread.
    /// </summary>
    public interface ICSharpCompiler
    {
        string CompilerVersion { get; }
        CompileResult Compile(CompileRequest request);
    }
}
