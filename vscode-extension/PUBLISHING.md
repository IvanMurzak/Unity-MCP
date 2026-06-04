# Publishing Handoff

This extension is prepared for local VSIX packaging, but it is intentionally not configured for publishing from a personal account.

## Before Publishing

1. Replace the temporary `publisher` value in [package.json](/Users/suporte/Unity-MCP/vscode-extension/package.json) with the official Marketplace publisher id.
2. Confirm the extension `name` and `displayName` are acceptable and unique for the Marketplace listing.
3. Review the Marketplace icon in `media/marketplace-icon.png`.
4. Review `README.md`, `CHANGELOG.md`, and `SUPPORT.md` for the final public wording.
5. Decide whether the first Marketplace release should remain marked as preview.

## Verification

Run from `/Users/suporte/Unity-MCP/vscode-extension`:

```bash
npm install
npm run build
npm test
npm run package:vsix
```

Then install the generated VSIX in a normal VS Code window and verify:

- Activity Bar dashboard loads
- status bar item appears
- `Check Status` works
- `Install Plugin` works
- `Configure Project` works
- `Open Unity` works

## Publish

Use the official publisher account and the VS Code publishing tooling to publish from the extension folder. The official documentation is:

- [Publishing Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)

## Notes

- The package metadata already includes `README.md`, `CHANGELOG.md`, `LICENSE.txt`, `SUPPORT.md`, and a PNG icon for Marketplace readiness.
- The current `publisher` value is only a placeholder for local packaging and should not be used for the real release.
