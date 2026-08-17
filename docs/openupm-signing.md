# OpenUPM Package Signing

Unity 6.3 introduced a package-signature check that surfaces a trust warning for
unsigned UPM packages installed from third-party registries (including OpenUPM).
This document describes how `IvanMurzak/Unity-MCP` signs its
`com.ivanmurzak.unity.mcp` package so the warning no longer appears in Unity 6.3+.

Tracks issue [#414](https://github.com/IvanMurzak/Unity-MCP/issues/414).

## How signing works

OpenUPM does **not** sign packages on behalf of authors — each package author runs
the signing flow in their own CI using a Unity organization's service account. The
signed `.tgz` is uploaded as a GitHub Release asset, and OpenUPM picks it up when
the package's listing has `trackingMode: githubRelease`.

References:
- <https://openupm.com/docs/signing-upm-packages.html>
- <https://openupm.com/blog/signing-upm-packages-with-openupm/>
- Reference workflow / repo layout: <https://github.com/openupm/com.example.signed-upm>

## What this repo ships

The signing step is implemented as the `build-signed-upm-package` job in
[`.github/workflows/release.yml`](../.github/workflows/release.yml). It runs in
parallel with tests and builds on every version-bump release commit, packs the
package at `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/` with Unity's
UPM CLI, verifies the resulting archive contains `package/.attestation.p7m` and
that its basename begins with `com.ivanmurzak.unity.mcp-`, and uploads the
signed `.tgz` as a `signed-upm-package` workflow artifact.

The artifact is then consumed by the atomic publish step in `release-unity-plugin`,
which downloads every release asset (the `.unitypackage`, the server `.zip`s, and
the signed `.tgz`) and creates the GitHub Release + tag with all assets attached
in a single `softprops/action-gh-release@v2` call. There are no separate
post-release publish jobs — the release is created in a single step after all
prerequisites pass; if any prerequisite fails, no release is created. The
`fail_on_unmatched_files: true` option on the release action ensures the step
hard-fails (rather than silently publishing) if any of the asset globs match
zero files.

### Signing is a hard gate on the release

`build-signed-upm-package` is **not** `continue-on-error`. If the three required
repo secrets (see below) are missing, or if `upm pack` / attestation verification
fails for any reason, the job exits non-zero, the release-creation jobs do not
run, and **no GitHub Release is created**. This is intentional: every public
release must ship the signed UPM tarball so OpenUPM (with the listing on
`trackingMode: githubRelease`) can surface the signed package without ever
race-publishing an unsigned git tag.

If you need to ship a release without signing, the correct action is to land a
follow-up PR that explicitly removes the gate — not to silently skip signing.

## One-time setup (repository owner)

These steps are operational, not code changes. The release pipeline cannot ship
a release until they are complete.

### 1. Create a Unity organization service account

A Unity organization is required to obtain UPM signing credentials (the
individual / personal Unity license cannot sign packages).

1. Go to the [Unity Cloud Dashboard](https://cloud.unity.com/) and either create
   an organization or use an existing one you own.
2. Inside the organization settings, create a service account dedicated to
   package signing.
3. Grant the service account the **package signing** permission for the
   organization.
4. Generate a service-account key — record the `Key ID`, the `Key Secret`, and
   the organization's `Org ID`. The secret is shown only once.

### 2. Add the three GitHub repository secrets

In this repo's Settings → Secrets and variables → Actions, add:

| Secret name                       | Value                                |
| --------------------------------- | ------------------------------------ |
| `UPM_SERVICE_ACCOUNT_KEY_ID`      | Service account key ID               |
| `UPM_SERVICE_ACCOUNT_KEY_SECRET`  | Service account key secret           |
| `UPM_ORG_ID`                      | Unity organization ID                |

CLI equivalent:

```bash
gh secret set UPM_SERVICE_ACCOUNT_KEY_ID     --repo IvanMurzak/Unity-MCP
gh secret set UPM_SERVICE_ACCOUNT_KEY_SECRET --repo IvanMurzak/Unity-MCP
gh secret set UPM_ORG_ID                     --repo IvanMurzak/Unity-MCP
```

### 3. The OpenUPM listing change — ✅ ALREADY DONE

> **This step is complete. Do not re-file it.** Read live from
> `https://raw.githubusercontent.com/openupm/openupm/master/data/packages/com.ivanmurzak.unity.mcp.yml`
> (HTTP 200) on **2026-08-17**, the listing already carries **both** required values:
>
> ```yaml
> trackingMode: githubRelease
> githubReleaseAssetName: 'com.ivanmurzak.unity.mcp-'
> ```
>
> An earlier revision of this document stated the listing "currently has
> `trackingMode: git`" and "must be flipped". **That was true when written and is now
> stale.** It was corrected here after a live read, because acting on it would have meant
> opening a redundant PR against `openupm/openupm` — or, worse, treating a satisfied
> precondition as a release blocker.

The listing lives in the [openupm/openupm](https://github.com/openupm/openupm)
repository at `data/packages/com.ivanmurzak.unity.mcp.yml`, and only a PR there can change
it — the value is registry-side, so nothing in this repository controls it.

**What `trackingMode: githubRelease` means for a release.** OpenUPM republishes the
`.tgz` **release asset** instead of packing the git tag, so the signed tarball is what
Unity users receive. Two consequences worth keeping in mind:

- The `com.ivanmurzak.unity.mcp-<version>.tgz` asset is **on OpenUPM's critical path**. A
  release whose signed tarball failed to upload publishes a tag OpenUPM cannot ingest.
  (Under the old `trackingMode: git` the tag alone was sufficient — that is no longer the
  shape to reason about.)
- The `githubReleaseAssetName` prefix guard makes OpenUPM select the tarball by filename
  rather than guessing from the asset list. This matters because a release also ships
  `.unitypackage` and may add further `.tgz` assets later.

**Verified against the live releases:** release `0.88.0` carries exactly two assets —
`com.ivanmurzak.unity.mcp-0.88.0.tgz` (756,589 bytes) and
`AI-Game-Dev-Installer.unitypackage` (27,304 bytes) — and `package.openupm.com` serves
`dist-tags.latest = 0.88.0`, so the ingest path is demonstrably working end to end.

Note also that this repository's release tags are **bare** (`0.88.0`), with no `v` prefix:
`release.yml`'s `check-version-tag` job passes `tag: ${{ steps.get_version.outputs.current-version }}`
straight to `mukunku/tag-exists-action`. A pre-release check for `v<version>` would look at
a tag namespace this repo has never used and would always report "absent".

## Verifying signing worked

After the next release ships:

1. Go to the [release page](https://github.com/IvanMurzak/Unity-MCP/releases)
   for the new version and confirm a `com.ivanmurzak.unity.mcp-<version>.tgz`
   asset is attached alongside the `.unitypackage` and server `.zip`s. The
   single-step publish runs only after the signed tarball is built and verified,
   so a successful release run should always include the signed asset.
2. Inspect the tarball locally to confirm it contains the signing attestation:

   ```bash
   curl -fsSL -o package.tgz \
     https://github.com/IvanMurzak/Unity-MCP/releases/download/<version>/com.ivanmurzak.unity.mcp-<version>.tgz
   tar -tzf package.tgz | grep '\.attestation\.p7m$'
   # expected: package/.attestation.p7m
   ```

3. Once the OpenUPM listing change merges, install the package in Unity 6.3+
   from OpenUPM and confirm the unsigned-package warning no longer appears.

## Troubleshooting

- **`build-signed-upm-package` fails with `UPM signing secrets are not configured`** —
  the three repo secrets above have not been set (or were set on the wrong repo).
  Complete the "One-time setup" steps above. The release pipeline is hard-gated
  on these secrets; until they are configured no release will ship.
- **`upm pack` fails with an authentication error** — the service account key
  is invalid or lacks the package-signing permission. Regenerate the key in the
  Unity org dashboard and re-set the GitHub secrets.
- **The release contains the `.tgz` but Unity 6.3 still shows the warning** —
  the OpenUPM listing is still on `trackingMode: git` (OpenUPM is serving the
  unsigned git-packed version, not the release asset). File the
  `openupm/openupm` PR described above.
