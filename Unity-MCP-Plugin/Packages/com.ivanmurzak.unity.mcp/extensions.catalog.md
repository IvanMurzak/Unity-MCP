# `extensions.catalog.json` — the shared extension catalogue

`extensions.catalog.json` (this file's sibling) is the **single source of truth** for the ten shipped
Unity-MCP extensions. It ships **inside this UPM package**, so it reaches every Unity user through
OpenUPM, and it is mirrored into the `unity-mcp-cli` npm package so that CLI stays self-contained.

## Why a JSON file when the editor already had a C# array

The editor's Extensions section has always been driven by a **hardcoded C# array** —
`MainWindowEditor._extensions` in
`Editor/Scripts/UI/Window/MainWindowEditor.Extensions.cs`. That array is load-bearing: it builds the
`ExtensionPanel` list in the *AI Game Developer* window and feeds
`ExtensionPanel.AddToManifest`, the real installer that writes the OpenUPM scoped registry and the
package dependency into a consumer's `Packages/manifest.json`.

When `unity-mcp-cli` gained `installExtension`, a **second** independent list of the same ten
extensions came into existence on the TypeScript side. Two hand-maintained lists of the same data
drift silently — so this JSON was introduced as the shared spec both sides must match, and two
build-failing parity tests were added to enforce it.

## The three artifacts and who consumes them

| Artifact | Path | Consumer | Ships via |
|---|---|---|---|
| **JSON source of truth** | `extensions.catalog.json` | the spec both code artifacts are tested against | this UPM package (OpenUPM) |
| **C# array** | `Editor/Scripts/UI/Window/MainWindowEditor.Extensions.cs` § `_extensions` | the editor Extensions UI + `ExtensionPanel.AddToManifest` | this UPM package (OpenUPM) |
| **TypeScript mirror** | `cli/src/utils/extensions-catalog.ts` § `EXTENSIONS_CATALOG` | `unity-mcp-cli`'s `installExtension` (library + `install-extension` command) | npm |

The C# array is **deliberately not refactored to read this JSON at editor runtime.** Doing so would
put a new file-load path in front of the extension list for every existing Unity user, and a failure
there degrades silently to an empty list. The parity tests below give the same anti-drift guarantee
with none of that risk. If a future change *does* move the C# side onto the JSON, the
`json ↔ C#` parity test becomes redundant rather than wrong.

## The parity tests (both build-failing)

Both live in the CLI's vitest suite — they need no Unity licence and run on every PR via
`test_cli.yml`, and again inside the publish job (`deploy.yml`), which is what makes them a release
gate rather than a nicety.

- **`cli/tests/extensions-catalog-parity.test.ts`** — `json ↔ TypeScript mirror`. Deep-equals the
  normalised JSON against `EXTENSIONS_CATALOG`, all six fields including `version`.
- **`cli/tests/extensions-catalog-csharp-parity.test.ts`** — `json ↔ C# array`. Parses the
  `_extensions` initialiser out of the C# source and deep-equals it against the JSON on the five
  fields the C# `ExtensionData` struct carries (`name`, `description`, `packageId`, `gitUrl`,
  `tools[]`).

Adding, removing, or editing an extension therefore means editing **the JSON and both code
artifacts**; either test fails until all three agree.

## `version` is `null` on every entry, on purpose

No entry pins an extension version. Every extension resolves OpenUPM's `dist-tags.latest` **live at
install time** — the CLI via `resolveLatestPackageVersion` in `cli/src/utils/manifest.ts`, the editor
via `ExtensionPanel.FetchLatestOpenUpmVersionAsync`. A catalogue that pinned versions would go stale
on the very next extension release, and this catalogue ships inside a package whose release cadence is
independent of the ten extensions'.

`version` has no counterpart in the C# `ExtensionData` struct, which is why the `json ↔ C#` test
compares five fields rather than six. That is not a gap: the editor has no version field *because* it
always resolves latest live, which is exactly what `version: null` encodes on the CLI side. A stray
pin in the JSON is still caught — by the `json ↔ TypeScript` test, and by a dedicated assertion in
the parity suite that every entry's `version` is `null`.
