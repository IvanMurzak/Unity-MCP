# Publishing Handoff

This extension is ready for local VSIX packaging and maintainer handoff, but it is intentionally not configured to publish from a personal account.

## Before Publishing

1. Replace the temporary `publisher` value in `package.json` with the official Marketplace publisher id.
2. Confirm `name`, `displayName`, and the Marketplace description are acceptable for the public listing.
3. Confirm whether the first public release should keep `preview: true`.
4. Review `README.md`, `CHANGELOG.md`, `SUPPORT.md`, and `DEVELOPMENT.md`.
5. Review `media/marketplace-icon.png`.
6. Verify the package still builds, tests, and packages cleanly.

## Verification Checklist

Run from the `vscode-extension/` directory:

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
- `Open Unity With MCP` works after Unity has initialized the project config

## Publish

Use the official publisher account and the VS Code publishing tooling from the extension folder. Official documentation:

- [Publishing Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)

## Notes

- The package metadata already includes README, changelog, support guidance, license, and a Marketplace PNG icon.
- The current `publisher` value is only a handoff placeholder and should not be used for the real Marketplace release.
- If packaging changes, rerun `npm run package:vsix` before publishing.
