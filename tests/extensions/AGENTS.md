# tests/extensions/

- `Editor/` — Unity-generated metadata, NUnit compiler-isolation tests and assembly definition.
- Shared test preparation copies this folder into each version-specific project's Assets.
- Keep compiler tests outside the shipped core/extension packages and fail on missing assemblies.
