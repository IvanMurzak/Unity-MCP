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
run ever reports the license as invalid, repeat steps 1–4 to refresh it.

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
