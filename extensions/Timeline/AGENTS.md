# AGENTS.md — Unity-AI-Timeline

Unity package `com.ivanmurzak.unity.mcp.timeline` — **Unity Timeline** (`com.unity.timeline`) for AI agents. It
ships no MCP tools: an RQL module for Unity-MCP's `rql-query` / `rql-mutate` (create `TimelineAsset`s, add/remove tracks,
add and time clips, place markers, bind a `PlayableDirector` and its per-track scene bindings). An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the former tool → RQL table is in `README.md`.

## Layout and commands

- `Packages/com.ivanmurzak.unity.mcp.timeline/` — shipped UPM sources, asmdefs and original tests.
- `Installer/` — preserved installer assets/tests; shared projects materialize them.
- `README.md`, `docs/` — module documentation; `migration-source.json` records provenance.
- Shared commands/workflow: `../../commands/extensions.py`, `../../docs/dev/extensions.md`.
- Use the shared per-version `../../Unity-Tests/` projects; do not create separate extension editor runs.

## Critical invariants

- **RQL surface.** Agents use Unity-MCP's `rql-query` / `rql-mutate`; operations are adapter
  methods on the owner type (TimelineAsset.AddTrack, TrackAsset.AddClip / AddMarker / Delete, TimelineClip.Delete,
  Marker.Delete), owner-less creation lives on the `timeline` source (`[RqlCallable]`), and every example belongs in the
  module guide (`TimelineModule.Guide`, rendered into the `unity-rql` skill).
- **Plain-class writes.** `TimelineClip` is not a `UnityEngine.Object`, so the engine cannot record or dirty it: its
  adapter wraps every writable member to `Undo.RecordObject` the parent track and dirty track + timeline. Do the same for
  any new non-Object Timeline type.
- **Asset-backed.** TimelineAssets are project assets (`.playable`); tracks, clips' PlayableAssets and markers are its
  sub-assets. RQL edits mark them dirty (no `SaveAssets`); Timeline's own create/delete calls register their Undo steps.
- **Hidden internals.** Private `m_*` serialized fields of TimelineAsset / TrackAsset / Marker (m_Tracks, m_Clips …)
  are hidden: they are only consistent when changed through Timeline's API.
- Every C# file starts with the copyright box header and `#nullable enable`.
