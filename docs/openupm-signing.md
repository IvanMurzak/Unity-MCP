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

The signing step is implemented as the `sign-and-publish-upm` job in
[`.github/workflows/release.yml`](../.github/workflows/release.yml). It runs on
every successful version-bump release (the same trigger as `publish-mcp-server`
and `publish-unity-installer`), packs the package at
`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/` with Unity's UPM CLI, and
attaches the resulting signed `.tgz` to the release tag.

The job is declared `continue-on-error: true` and exits early with a warning if
the required Unity-org secrets are not configured — so until the secrets are in
place the existing release flow continues to ship unchanged.

## One-time setup (repository owner)

These steps land outside this PR — they are operational, not code changes.

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

### 3. File the OpenUPM listing change

OpenUPM's package listing for `com.ivanmurzak.unity.mcp` currently has
`trackingMode: git`, which makes OpenUPM pack and serve unsigned tarballs from
the repository's git tags. To make OpenUPM serve the signed tarball that the
workflow now uploads, the listing must be flipped to `trackingMode: githubRelease`.

The listing lives in the [openupm/openupm](https://github.com/openupm/openupm)
repository at `data/packages/com.ivanmurzak.unity.mcp.yml`. Open a PR there
changing:

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

`githubReleaseAssetName` is **not** required — the release has only one
`.tgz` / `.tar.gz` asset (the signed UPM tarball), so OpenUPM will auto-select
it. If the release ever ships multiple `.tgz` assets, add:

```yaml
githubReleaseAssetName: 'com.ivanmurzak.unity.mcp-'
```

so OpenUPM picks the right one by filename prefix.

## Verifying signing worked

After the next release that runs with the secrets in place:

1. Go to the [release page](https://github.com/IvanMurzak/Unity-MCP/releases)
   for the new version and confirm a `com.ivanmurzak.unity.mcp-<version>.tgz`
   asset is attached alongside the existing `.unitypackage` and server `.zip`s.
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

- **Job is skipped with warning "UPM signing secrets are not configured"** —
  expected when the three secrets are not yet set. Complete the
  "One-time setup" steps above.
- **`upm pack` fails with an authentication error** — the service account key
  is invalid or lacks the package-signing permission. Regenerate the key in the
  Unity org dashboard and re-set the GitHub secrets.
- **The release contains the `.tgz` but Unity 6.3 still shows the warning** —
  the OpenUPM listing is still on `trackingMode: git` (OpenUPM is serving the
  unsigned git-packed version, not the release asset). File the
  `openupm/openupm` PR described above.
