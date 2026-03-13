---
name: build-cli
description: "Build the unity-mcp-cli TypeScript CLI tool and link it globally for terminal use."
disable-model-invocation: true
argument-hint: "[optional: --link to also run npm link]"
---

# Build CLI

Build the `unity-mcp-cli` TypeScript project and optionally link it globally.

## Step 1 — Install Dependencies

```bash
cd cli && npm install
```

Only needed if `node_modules/` is missing or `package.json` changed.

## Step 2 — Build TypeScript

```bash
cd cli && npm run build
```

This compiles `src/**/*.ts` → `dist/**/*.js` via `tsc`.

## Step 3 — Link Globally (if `$ARGUMENTS` contains `--link` or first time)

```bash
cd cli && npm link
```

This creates a global symlink so `unity-mcp-cli` is available from any terminal. Only needed once — after that, `npm run build` alone is sufficient.

## Step 4 — Verify

```bash
unity-mcp-cli --version
```

Confirm the CLI is accessible and shows the expected version.

## Report

Print the version and confirm success. If any step failed, show the error output.
