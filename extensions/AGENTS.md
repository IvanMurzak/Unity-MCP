# extensions/

- `manifest.json` — inventory of independently versioned UPM packages and tag prefixes.
- `<Feature>/Packages/<package-id>/` — shipped package, assembly definitions and original tests.
- `<Feature>/Installer/` — preserved installer project and its tests; no separate release workflow.
- `<Feature>/README.md`, `docs/` — module documentation; `migration-source.json` records original bytes.
- `../templates/extension/` — inert scaffold; never include it in tests or publication.

Use `commands/extensions.py` for shared test-project preparation and component metadata.
Keep package IDs, assembly names and Unity-generated `.meta` GUIDs stable.
Never add sibling assembly references without the corresponding UPM dependency.
