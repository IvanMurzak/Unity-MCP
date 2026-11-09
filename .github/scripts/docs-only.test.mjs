// ┌──────────────────────────────────────────────────────────────────┐
// │  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
// │  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
// │  Copyright (c) 2026 Ivan Murzak                                  │
// │  Licensed under the Apache License, Version 2.0.                 │
// │  See the LICENSE file in the project root for more information.  │
// └──────────────────────────────────────────────────────────────────┘

// Run: node --test .github/scripts/docs-only.test.mjs

import assert from "node:assert/strict";
import { execFileSync, spawnSync } from "node:child_process";
import { mkdirSync, mkdtempSync, readFileSync, renameSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import { changedFiles, isDocsOnly, isDocsPath } from "./docs-only.mjs";

test("isDocsPath lists the root docs, memory, agent config and AGENTS.md, nothing else", () => {
  const skip = [
    "docs/README.ja.md",
    "docs/agents/release.md",
    "docs/img/promo/banner.gif",
    "memory/MEMORY.md",
    "AGENTS.md",
    ".claude/settings.json",
    ".claude/skills/x/SKILL.md",
    ".agents/skills/x/SKILL.md",
  ];
  const run = [
    ".claude", // a file, not the folder
    "cli/.claude/settings.json",
    ".claude-plugin/plugin.json",
    ".github/agents/x.agent.md",
    "README.md", // documentation-sync.md ties it to the shipped copies below
    "Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/README.md",
    "Installer/Assets/com.IvanMurzak/AI Game Dev Installer/README.md",
    "extensions/Animation/README.md", // commands/extensions.py reads it
    "cli/README.md", // the npm package ships these two
    "cli/docs/README.ja.md",
    "cli/AGENTS.md",
    "tests/filter-oracle/AGENTS.md",
    "Unity-MCP-Plugin/Tests~/EngineFree/README.md",
    "templates/AGENTS.md",
    "server.json",
    ".github/workflows/test_pull_request.yml",
    ".github/scripts/docs-only.mjs",
    "commands/extensions.py",
    "docs", // a file, not the folder
    "documents/a.md",
    "memory", // same
    "agents.md",
  ];
  for (const path of skip) assert.equal(isDocsPath(path), true, path);
  for (const path of run) assert.equal(isDocsPath(path), false, path);
});

test("isDocsOnly needs at least one file and every file on the list", () => {
  assert.equal(isDocsOnly(["docs/a.md", "AGENTS.md"]), true);
  assert.equal(isDocsOnly(["docs/a.md", "cli/package.json"]), false);
  assert.equal(isDocsOnly([]), false);
});

test("the workflow gates only the Unity legs, each so that only an explicit true skips it", () => {
  const workflow = readFileSync(fileURLToPath(new URL("../workflows/test_pull_request.yml", import.meta.url)), "utf8");
  const jobs = workflow
    .slice(workflow.indexOf("\njobs:\n"))
    .replace(/^\s*#.*\n/gm, "")
    .split(/\n {2}(?=[^\s#])/)
    .slice(1);
  const gated = jobs.filter((job) => /\bneeds:.*\bchanges\b/.test(job));
  assert.ok(gated.length > 0);
  for (const job of jobs) {
    const key = job.slice(0, job.indexOf(":"));
    assert.equal(gated.includes(job), key.startsWith("test-unity-"), key);
  }
  for (const job of gated) assert.ok(job.includes("if: ${{ !cancelled() && needs.changes.outputs.docs_only != 'true' }}"), job);

  // A skipped gate job could skip what depends on it, through the reusable workflows and release_extensions.yml.
  const gate = jobs.find((job) => job.startsWith("changes:"));
  assert.doesNotMatch(gate, /^ {4}if:/m);
});

function withRepo(check) {
  const repo = mkdtempSync(join(tmpdir(), "docs-only-"));
  const git = (...args) => execFileSync("git", ["-c", "user.name=t", "-c", "user.email=t@t", ...args], { cwd: repo, stdio: "pipe" });
  try {
    git("init", "-b", "main");
    check(repo, git);
  } finally {
    rmSync(repo, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); // Windows briefly keeps git's handles
  }
}

/** A repo with one base commit, then the merge commit of a branch `change` applied to it. */
function withMerge(change, check) {
  withRepo((repo, git) => {
    const write = (path) => {
      mkdirSync(dirname(join(repo, path)), { recursive: true });
      writeFileSync(join(repo, path), path);
    };
    write("cli/a.ts");
    write("cli/b.ts"); // keeps cli/ alive, or git reads a rename of a.ts as moving the folder
    git("add", ".");
    git("commit", "-m", "base");
    git("checkout", "-b", "pr");
    change({ repo, write });
    git("add", ".");
    git("commit", "-m", "pr");
    git("checkout", "main");
    write("cli/moved-on-main.ts");
    git("add", ".");
    git("commit", "-m", "main moved");
    git("merge", "--no-ff", "-m", "merge", "pr");
    check(repo);
  });
}

test("changedFiles lists only what the pull request changes, not what main gained", () => {
  withMerge(({ write }) => write("docs/a.md"), (repo) => assert.deepEqual(changedFiles(repo), ["docs/a.md"]));
});

test("a rename from code into docs lists both sides, so it is not docs-only", () => {
  withMerge(
    ({ repo }) => {
      mkdirSync(join(repo, "docs"));
      renameSync(join(repo, "cli/a.ts"), join(repo, "docs/a.ts"));
    },
    (repo) => {
      assert.deepEqual(changedFiles(repo).sort(), ["cli/a.ts", "docs/a.ts"]);
      assert.equal(isDocsOnly(changedFiles(repo)), false);
    },
  );
});

test("a non-ASCII docs path stays unquoted, so it still matches the list", () => {
  withMerge(({ write }) => write("docs/заметка.md"), (repo) => assert.equal(isDocsOnly(changedFiles(repo)), true));
});

test("changedFiles refuses a HEAD that is not a merge commit", () => {
  withRepo((repo, git) => {
    git("commit", "--allow-empty", "-m", "first");
    git("commit", "--allow-empty", "-m", "second");
    assert.throws(() => changedFiles(repo));
  });
});

test("the script writes the output only when HEAD is the commit under test", () => {
  const script = fileURLToPath(new URL("./docs-only.mjs", import.meta.url));
  withMerge(({ write }) => write("docs/a.md"), (repo) => {
    const output = join(repo, "output.txt");
    const run = (sha) => spawnSync(process.execPath, [script], { cwd: repo, encoding: "utf8", env: { ...process.env, GITHUB_OUTPUT: output, GITHUB_SHA: sha } });

    assert.notEqual(run("0".repeat(40)).status, 0);
    assert.throws(() => readFileSync(output));

    const head = execFileSync("git", ["rev-parse", "HEAD"], { cwd: repo, encoding: "utf8" }).trim();
    assert.equal(run(head).status, 0);
    assert.equal(readFileSync(output, "utf8"), "docs_only=true\n");
  });
});
