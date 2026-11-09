// ┌──────────────────────────────────────────────────────────────────┐
// │  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
// │  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
// │  Copyright (c) 2026 Ivan Murzak                                  │
// │  Licensed under the Apache License, Version 2.0.                 │
// │  See the LICENSE file in the project root for more information.  │
// └──────────────────────────────────────────────────────────────────┘

/**
 * CI gate: does this pull request change only documentation?
 *
 *   node .github/scripts/docs-only.mjs
 *
 * Writes `docs_only=true|false` to $GITHUB_OUTPUT; test_pull_request.yml skips its Unity legs only on
 * an explicit `true`. So every doubt means a full run: a path not listed below, an empty diff, or a
 * failure here, since this step is `continue-on-error` and leaves the output empty. HEAD must be
 * GITHUB_SHA, so a failed checkout cannot classify some other commit.
 *
 * Reads the diff of the merge commit GitHub builds for a pull_request run (HEAD^1 is the base), which
 * needs `fetch-depth: 2` on the checkout and no token or API. `--no-renames` because a rename from
 * code into docs/ would otherwise list only its docs/ side.
 *
 * To skip CI for more paths, edit isDocsPath and its table in docs-only.test.mjs. List only what no
 * build, test, packaging or generation step reads. These do not qualify: cli/docs/ and cli/README.md
 * (`files` of the npm package), the package and Installer READMEs (shipped in the UPM package and the
 * .unitypackage), extensions/<module>/README.md (commands/extensions.py reads it). The root README.md
 * has no reader today, but documentation-sync.md makes a change to it also touch those shipped copies,
 * so listing it would rarely skip anything. GitHub Pages publishes main:/docs after a merge; no PR job
 * reads or tests that site, so skipping the Unity legs loses nothing there. The root .claude/ and
 * .agents/ hold agent settings and skills that nothing reads; the plugin's ".claude/skills" is a path
 * in the user's project.
 */

import { execFileSync } from "node:child_process";
import { appendFileSync, realpathSync } from "node:fs";
import { fileURLToPath } from "node:url";

const git = (cwd, ...args) => execFileSync("git", args, { cwd, encoding: "utf8" });

/** @param {string} path repo-relative, forward slashes */
export function isDocsPath(path) {
  return path === "AGENTS.md" || /^(docs|memory|\.claude|\.agents)\//.test(path);
}

/** @param {readonly string[]} files */
export function isDocsOnly(files) {
  return files.length > 0 && files.every(isDocsPath);
}

/** The files the pull request's merge commit changes against its base. */
export function changedFiles(repo) {
  git(repo, "rev-parse", "--verify", "HEAD^2"); // throws unless HEAD is the merge commit
  return git(repo, "diff", "--name-only", "--no-renames", "-z", "HEAD^1", "HEAD").split("\0").filter(Boolean);
}

function main() {
  const head = git(process.cwd(), "rev-parse", "HEAD").trim();
  if (head !== process.env.GITHUB_SHA) throw new Error(`HEAD ${head} is not GITHUB_SHA ${process.env.GITHUB_SHA}`);
  const files = changedFiles(process.cwd());
  const docsOnly = isDocsOnly(files);
  const other = files.find((file) => !isDocsPath(file));
  console.log(`${files.length} changed file(s)${other ? `, e.g. ${other} is outside the docs list` : ""}`);
  console.log(`docs_only=${docsOnly}`);
  appendFileSync(process.env.GITHUB_OUTPUT, `docs_only=${docsOnly}\n`);
}

// realpath: import.meta.url has its symlinks resolved, argv[1] does not, and a mismatch would exit 0 with no output.
if (process.argv[1] && realpathSync(process.argv[1]) === fileURLToPath(import.meta.url)) main();
