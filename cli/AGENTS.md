# cli/AGENTS.md

`unity-mcp-cli` npm package (TypeScript, ESM): CLI binary plus a programmatic library.
Published only by the `deploy-cli-to-npm` job of the repo's `release.yml`; its version moves with
the plugin's (`bump-version.ps1`).

- `src/cli.ts`, `bin/` — CLI entry (commander).
- `src/commands/` — one file per CLI command; parse args, delegate the work to `src/lib/`.
- `src/lib/` + `src/lib.ts` — library API (`exports` in `package.json`). `lib.ts` must have no
  top-level side effects and no `commander` import; results are `kind: 'success' | 'failure'`
  unions, errors are never thrown past the public boundary (rules in the `lib.ts` header).
- `src/utils/` — shared helpers (Unity Hub/editor/process, ports, credentials, server version).
- `tests/` — vitest, one `<area>.test.ts` per module; shared helpers in `tests/helpers/`.
- `docs/` — README translations.

Build/test: `npm ci && npm test` (`pretest` builds with `tsc`).
