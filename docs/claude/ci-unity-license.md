# Unity license in CI (stored `.ulf` — GameCI standard)

Unity CI (tests + Installer build + the Claude/Copilot MCP jobs) needs an activated
Unity **Personal** license. We use the **GameCI-standard stored-license** approach:
a `.ulf` license file is generated **once** and kept in the `UNITY_LICENSE` secret;
game-ci consumes it directly on every run. **No per-run web scraping.**

## Why this design (history)

Previously a composite action (`.github/actions/unity/activate-license`) ran the
`unity-license-activate` Puppeteer bot, which logged into Unity's website **every CI
run** to convert a fresh `.alf` into a `.ulf`. Unity redesigned their sign-in page
(email-first SSO + a cookie-consent modal), the scraper's `waitForSelector` timed
out on all retries, and **every Unity CI leg failed**. The scraper was inherently
fragile and also carried a hard-coded Unity email/password in this public repo.

The stored-`.ulf` flow removes the scraper entirely and is what GameCI documents for
Personal licenses.

## Required secrets

Set all **three** (game-ci needs all three even for a Personal license):

| Secret | Value |
|---|---|
| `UNITY_LICENSE` | Full contents of the `.ulf` license file (XML) |
| `UNITY_EMAIL` | The CI Unity account email |
| `UNITY_PASSWORD` | The CI Unity account password |

`IvanMurzak` is a **personal account** (GitHub org-level secrets are not available),
so set these **per-repo** — repeat on each Unity repo that runs Unity CI:

```bash
gh secret set UNITY_LICENSE  --repo IvanMurzak/Unity-MCP < Unity_vXXXX.ulf
gh secret set UNITY_EMAIL    --repo IvanMurzak/Unity-MCP
gh secret set UNITY_PASSWORD --repo IvanMurzak/Unity-MCP
```

## Fork pull requests get no secrets (read this before "the license is broken")

GitHub withholds repository secrets from a `pull_request` run whose head repository is a
**fork**. `secrets: inherit` still resolves — to empty strings. So on a fork PR
`game-ci/unity-test-runner` receives an empty `UNITY_LICENSE` and aborts before it pulls an
image or contacts Unity, and **all 12 `test-unity-*` legs go red at once** (6 caller jobs in
`test_pull_request.yml` × the 2-way `platform: [base, windows-mono]` matrix).

**This is the expected behaviour of an unlicensed run.** The license, the secrets and the
workflow are all fine; a fork simply cannot see them. Nothing here needs fixing, and
re-issuing the `.ulf` will not change it.

### How to tell it apart from a real license failure

The tell is **duration**, and it survives log expiry — job metadata outlives logs, so
`gh api repos/IvanMurzak/Unity-MCP/actions/runs/<run-id>/jobs` answers it even when the log
download returns HTTP 410:

- **No secrets (fork PR)** — the `Run game-ci/unity-test-runner@…` step completes **within a
  second** of starting (usually the same second; at a boundary, the next one), while
  `Free disk space` and `actions/cache` before it succeed. The whole job is 1–4 minutes,
  nearly all of it disk cleanup.
- **A genuinely bad `.ulf`** — the step runs for **minutes**. For scale, on the licensed run
  `33757559794` the same step takes 9–13 minutes per leg before it succeeds. (Why it takes that
  long — image pull, then activation inside the container — is unverified here: no bad-`.ulf`
  run is on record. If you hit one, record its run id in this section.)

Duration separates *empty* from *bad*. It does **not** separate a fork PR from a
same-repository run whose `UNITY_LICENSE` was deleted or never set — the secret is declared
`required: false`, so both deliver an empty string and both fail identically. Settle that with
the head-repository check, not the clock:

```bash
gh api repos/IvanMurzak/Unity-MCP/actions/runs/<run-id> \
  --jq '"\(.event) \(.head_repository.full_name) \(.conclusion)"'
```

A `full_name` other than `IvanMurzak/Unity-MCP` means the run had no secrets.

One more fork-PR shape to expect, with a different signature: a run stuck at
`action_required` has **not started at all** — GitHub holds a first-time contributor's workflow
for maintainer approval. There is then no red to diagnose and `conclusion` comes back `null`.
Approve the run first; the no-secrets shape above is what you should see once it does start.

### How to run the licensed suite against a fork PR's code

`test_pull_request_manual.yml` is `workflow_dispatch`-only, so it runs with full secrets:

```bash
gh workflow run test_pull_request_manual.yml --repo IvanMurzak/Unity-MCP --ref main
```

Reviewing a fork PR's actual code this way needs a ref the maintainer picks — a fork PR's
commits are fetchable from this repository as `refs/pull/<n>/head` (always) and
`refs/pull/<n>/merge` (only while GitHub can compute a clean test-merge, so absent on a
conflicting PR). Neither is fetched by default — `git fetch origin refs/pull/<n>/head` — and
neither can be dispatched: `workflow_dispatch` takes only a branch or tag as its ref, so
`--ref refs/pull/<n>/head` is rejected.
Wiring that into the dispatch (and deciding whether a fork's Unity checks should be allowed to
pass at all, given the required-status-check ruleset) is a **policy decision** — deliberately not
settled by this document. It is open in **PR #971**; issue #543, which first asked for it, was
closed as completed back in 2026-03, so follow the PR, not the issue.

### Measured, 2026-09-03 (issue #973)

All seven `test-pull-request` runs between 2026-08-25 and 2026-08-29 came from a fork
(`Nghaiz`, `akimaleo`, `zorionarrillaga`) and none went green — 3 `failure`, 2 `cancelled` and
2 `action_required`, the last being the approval gate above. The last same-repository run before them,
`32788655839` (2026-08-24), was green, and there was no same-repository PR in between. Read as a
time series that looks exactly like a CI regression, and it was not one. Three runs pin it:

| run | what it was | result |
|---|---|---|
| [`32992648441`](https://github.com/IvanMurzak/Unity-MCP/actions/runs/32992648441) | fork PR, secrets withheld | 12/12 legs red; test-runner step `04:57:53Z -> 04:57:53Z` |
| [`33758511822`](https://github.com/IvanMurzak/Unity-MCP/actions/runs/33758511822) | `test_pull_request_manual.yml` dispatched from a throwaway branch with one Unity job's `secrets: inherit` removed — the one variable | red, same instant-failure shape |
| [`33757559794`](https://github.com/IvanMurzak/Unity-MCP/actions/runs/33757559794) | dispatch on `main`, secrets present, same commit `91e2472a` the fork PRs branched from | green |

## The one machine-binding gotcha (why a desktop `.ulf` fails)

A `.ulf` binds to the **HardwareId** of the machine whose `.alf` produced it.
**All GitHub-hosted runners report the same HardwareId**, so a `.ulf` generated from
a **CI-generated** `.alf` is valid across every CI run. A `.ulf` generated from an
`.alf` created on your **desktop** binds to your local machine and fails in CI with a
machine-binding mismatch. **Always generate the `.alf` in CI** (the workflow below
does exactly this).

## One-time setup / refresh procedure

1. **Generate a CI `.alf`.** Actions tab → **generate-unity-activation-file** → *Run
   workflow* (default Unity version is fine — the `.ulf` is version-independent).
2. **Download** the `unity-activation-file` artifact from that run → you get a `.alf`.
3. **Convert to `.ulf`.** Open <https://license.unity3d.com/manual>, sign in with the
   CI Unity account, upload the `.alf`, choose **Unity Personal**, download the `.ulf`.
4. **Store it.** `gh secret set UNITY_LICENSE --repo IvanMurzak/Unity-MCP < Unity_vXXXX.ulf`
   (and make sure `UNITY_EMAIL` / `UNITY_PASSWORD` exist too).
5. Re-run any failed Unity workflow — it now uses the stored license.

Personal `.ulf` files carry `ValidTo="9999-12-31"` and effectively don't expire; if a
run ever reports the license as invalid, repeat steps 1–4 to refresh it — but rule out a fork
PR first ("Fork pull requests get no secrets" above), which reports the same thing and is not
fixed by refreshing.

## Security note

The old scraper embedded a Unity account email + password directly in this public
repo. That has been removed. **Rotate that Unity account's password** and put the new
value in the `UNITY_PASSWORD` secret. Never commit credentials to the repo again.

## Where it's wired

- `.github/workflows/test_unity_plugin.yml` — test matrix; `game-ci/unity-test-runner`
  reads `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD` from its `env:` block.
- `.github/workflows/release.yml` (`build-unity-installer`) — Installer test + export.
- `.github/actions/setup-unity-mcp/action.yml` — writes the `UNITY_LICENSE` `.ulf` into
  the license folder mounted into the Unity Editor container (used by `claude.yml` and
  `copilot-setup-steps.yml`).
- `.github/workflows/generate-unity-activation-file.yml` — the one-shot `.alf` generator.
