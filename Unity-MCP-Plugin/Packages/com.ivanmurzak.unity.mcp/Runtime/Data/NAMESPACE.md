# Namespace convention for `Runtime/Data/`

Every C# type in this folder MUST declare:

```csharp
namespace Unity.MCP.Data
```

This **overrides** the parent asmdef's auto-generated
`rootNamespace` (`com.IvanMurzak.Unity.MCP.Runtime`). When you
create a new file via Unity's "Create > C# Script" menu, Unity
will pre-fill the namespace as `com.IvanMurzak.Unity.MCP.Runtime.Data`
— delete that and replace it with `Unity.MCP.Data`.

## Why

These data model types (the `ObjectRef` hierarchy, `*Data`,
`*Shallow`, `*Metadata`, `*Ref`, `*List`, `PathPatch`, …) are
exposed to AI agents through MCP tool JSON Schema generation.
Their **fully qualified type names appear verbatim** as `$defs`
keys and `$ref` paths in every tool's schema. A short namespace
keeps those schemas compact and intuitive for AI agents.

See `.specify/memory/constitution.md` § IV (Naming Conventions)
for the authoritative rule.
