# OpenUPM Package Signing

Unity 6.3 introduced a package-signature check that surfaces a trust warning for
unsigned UPM packages installed from third-party registries (including OpenUPM).
This document describes how `IvanMurzak/Unity-AI-ParticleSystem` signs its
`com.ivanmurzak.unity.mcp.particlesystem` package so the warning no longer
appears in Unity 6.3+.

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
package at `Unity-Package/Packages/com.ivanmurzak.unity.mcp.particlesystem/` with Unity's UPM CLI, verifies the
resulting archive contains `package/.attestation.p7m` and that its basename begins
with `com.ivanmurzak.unity.mcp.particlesystem-`, and uploads the signed `.tgz` as
a `signed-upm-package` workflow artifact.

The artifact is then consumed by the atomic publish step in `release-unity-plugin`,
which downloads every release asset (the `.unitypackage` and the signed `.tgz`)
and creates the GitHub Release + tag with all assets attached in a single
`softprops/action-gh-release@v2` call. There are no separate post-release publish
jobs — the release is created in a single step after all prerequisites pass; if
any prerequisite fails, no release is created. The `fail_on_unmatched_files: true`
option on the release action ensures the step hard-fails (rather than silently
publishing) if any of the asset globs match zero files.

### Signing is a hard gate on the release

`build-signed-upm-package` is **not** `continue-on-error`. If the three required
repo secrets (see below) are missing, or if `upm pack` / attestation verification
fails for any reason, the job exits non-zero, the release-creation job does not
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
gh secret set UPM_SERVICE_ACCOUNT_KEY_ID     --repo IvanMurzak/Unity-AI-ParticleSystem
gh secret set UPM_SERVICE_ACCOUNT_KEY_SECRET --repo IvanMurzak/Unity-AI-ParticleSystem
gh secret set UPM_ORG_ID                     --repo IvanMurzak/Unity-AI-ParticleSystem
```

### 3. File the OpenUPM listing change

OpenUPM's package listing for `com.ivanmurzak.unity.mcp.particlesystem` currently
has `trackingMode: git`, which makes OpenUPM pack and serve unsigned tarballs from
the repository's git tags. To make OpenUPM serve the signed tarball that the
workflow now uploads, the listing must be flipped to `trackingMode: githubRelease`.

The listing lives in the [openupm/openupm](https://github.com/openupm/openupm)
repository at `data/packages/com.ivanmurzak.unity.mcp.particlesystem.yml`. Open a
PR there changing:

```yaml
trackingMode: git
```

to:

```yaml
trackingMode: githubRelease
```

Per the OpenUPM blog, switch `trackingMode` to `githubRelease` **before** the
first signed release ships, so OpenUPM does not race-publish the unsigned git
tag in parallel.

Also set `githubReleaseAssetName` so OpenUPM picks the signed tarball by
filename prefix rather than guessing from the asset list. The release also ships
the `.unitypackage` installer asset, so the prefix guard prevents a future
breaking failure mode:

```yaml
githubReleaseAssetName: 'com.ivanmurzak.unity.mcp.particlesystem-'
```

## Verifying signing worked

After the next release ships:

1. Go to the
   [release page](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/releases)
   for the new version and confirm a
   `com.ivanmurzak.unity.mcp.particlesystem-<version>.tgz` asset is attached
   alongside the `.unitypackage`. The single-step publish runs only after the
   signed tarball is built and verified, so a successful release run should always
   include the signed asset.
2. Inspect the tarball locally to confirm it contains the signing attestation:

   ```bash
   curl -fsSL -o package.tgz \
     https://github.com/IvanMurzak/Unity-AI-ParticleSystem/releases/download/<version>/com.ivanmurzak.unity.mcp.particlesystem-<version>.tgz
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
