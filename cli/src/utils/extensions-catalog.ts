// The CLI's typed mirror of the SHARED extension catalogue — the single source of
// truth `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json`
// (see that file's sibling `extensions.catalog.md`).
//
// There are THREE artifacts holding this same list, and all three are kept in
// lockstep by build-failing parity tests:
//
//   1. `extensions.catalog.json`        — the JSON source of truth, shipped in the UPM package.
//   2. `MainWindowEditor._extensions`   — the C# array that drives the editor's Extensions
//                                         section and `ExtensionPanel.AddToManifest`.
//   3. `EXTENSIONS_CATALOG` (this file) — drives `installExtension` in this CLI.
//
// SINGLE SOURCE OF TRUTH: this constant MUST stay equivalent to the JSON. The parity
// test `cli/tests/extensions-catalog-parity.test.ts` reads the JSON and FAILS the build
// if this mirror drifts; `cli/tests/extensions-catalog-csharp-parity.test.ts` does the
// same for the C# array. Adding an extension = appending an entry to the JSON, the C#
// array, AND this array (both tests enforce it). This mirror exists so the published npm
// package stays self-contained — no runtime `../Unity-MCP-Plugin` dependency.
//
// No top-level side effects; pure data + pure lookups only.

/** One tool a catalogue extension contributes — mirrors the JSON `tools[]` entry. */
export interface ExtensionTool {
  readonly name: string;
  readonly description: string;
}

/**
 * One installable extension — the CLI analog of the C# `ExtensionPanel.ExtensionData`.
 * `packageId` is the INSTALL IDENTITY (the `Packages/manifest.json` dependency key,
 * resolved from the OpenUPM scoped registry).
 * `version` is `null` for a floating (unpinned) reference — every catalogue entry is
 * unpinned, so OpenUPM's `dist-tags.latest` is resolved live at install time.
 *
 * `tools` is the same CURATED highlight list the editor renders in an extension's
 * tooltip (`ExtensionPanel.BuildTooltip`) — it is deliberately NOT an exhaustive
 * inventory of the extension's MCP tools, and must not be consumed as one.
 */
export interface ExtensionDescriptor {
  readonly name: string;
  readonly description: string;
  readonly packageId: string;
  readonly version: string | null;
  readonly gitUrl: string | null;
  readonly tools: readonly ExtensionTool[];
}

/**
 * The extension catalogue, single-sourced from
 * `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json`.
 * Ten shipped extensions; `Unity-AI-Tools-Template` is a template and is excluded.
 */
export const EXTENSIONS_CATALOG: readonly ExtensionDescriptor[] = [
  {
    name: 'Animation',
    description: 'AI-driven animation control and playback tools.',
    packageId: 'com.ivanmurzak.unity.mcp.animation',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Animation.git',
    tools: [
      { name: 'animation-create', description: 'Create AnimationClip assets with keyframes' },
      { name: 'animation-get-data', description: 'Inspect clip curves, events, and properties' },
      { name: 'animation-modify', description: 'Edit curves, events, and settings on a clip' },
      { name: 'animator-create', description: 'Create AnimatorController assets' },
      { name: 'animator-get-data', description: 'Inspect controller layers, states, and parameters' },
      { name: 'animator-modify', description: 'Edit parameters, states, and transitions' },
    ],
  },
  {
    name: 'Cinemachine',
    description: 'AI-assisted Cinemachine camera setup and configuration tools.',
    packageId: 'com.ivanmurzak.unity.mcp.cinemachine',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Cinemachine.git',
    tools: [
      { name: 'cinemachine-camera-create', description: 'Create a CinemachineCamera in the scene' },
      { name: 'cinemachine-set-targets', description: 'Set the Follow and LookAt targets' },
      { name: 'cinemachine-set-lens', description: 'Configure FOV, clip planes, and dutch' },
      { name: 'cinemachine-set-body', description: 'Set the position-control component (Follow/Orbital/...)' },
      { name: 'cinemachine-set-noise', description: 'Add camera shake via Perlin noise' },
    ],
  },
  {
    name: 'InputSystem',
    description:
      'AI-assisted Unity Input System authoring: InputActionAssets, maps, actions, bindings, and control schemes.',
    packageId: 'com.ivanmurzak.unity.mcp.inputsystem',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-InputSystem.git',
    tools: [
      { name: 'inputsystem-asset-create', description: 'Create a new .inputactions InputActionAsset' },
      { name: 'inputsystem-actionmap-add', description: 'Add an ActionMap to the asset' },
      { name: 'inputsystem-action-add', description: 'Add an Action (type + expectedControlType)' },
      { name: 'inputsystem-binding-add', description: 'Add a binding path to an Action' },
      { name: 'inputsystem-binding-composite-add', description: 'Add a composite binding (2DVector/1DAxis)' },
      { name: 'inputsystem-controlscheme-add', description: 'Add a control scheme with device requirements' },
      { name: 'inputsystem-get', description: "Read the asset's maps, actions, and bindings" },
    ],
  },
  {
    name: 'Navigation',
    description: 'AI-driven NavMesh navigation: surfaces, baking, agents, and links.',
    packageId: 'com.ivanmurzak.unity.mcp.navigation',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Navigation.git',
    tools: [
      { name: 'navigation-surface-add', description: 'Add and configure a NavMeshSurface' },
      { name: 'navigation-set-bake-settings', description: 'Set agent radius/height/slope/step and voxel size' },
      { name: 'navigation-surface-bake', description: 'Bake or clear a NavMeshSurface' },
      { name: 'navigation-modifier-add', description: 'Add a NavMeshModifier (override area / ignore)' },
      { name: 'navigation-modifier-volume-add', description: 'Add a NavMeshModifierVolume' },
      { name: 'navigation-link-add', description: 'Add a NavMeshLink between two points' },
      { name: 'navigation-agent-add', description: 'Add and configure a NavMeshAgent' },
      { name: 'navigation-agent-set-destination', description: "Set a NavMeshAgent's destination" },
      { name: 'navigation-list', description: 'List NavMeshSurfaces and NavMeshAgents' },
      { name: 'navigation-get', description: 'Serialize any NavMesh component' },
      { name: 'navigation-modify', description: 'Modify any NavMesh component via ReflectorNet' },
    ],
  },
  {
    name: 'ParticleSystem',
    description: 'AI-powered particle system creation and control tools.',
    packageId: 'com.ivanmurzak.unity.mcp.particlesystem',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-ParticleSystem.git',
    tools: [
      { name: 'particle-system-get', description: 'Inspect ParticleSystem modules and settings' },
      { name: 'particle-system-modify', description: 'Modify emission, shape, color, noise, and more' },
    ],
  },
  {
    name: 'ProBuilder',
    description: 'AI-assisted ProBuilder geometry modeling tools.',
    packageId: 'com.ivanmurzak.unity.mcp.probuilder',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-ProBuilder.git',
    tools: [
      { name: 'probuilder-create-shape', description: 'Create editable 3D primitives in the scene' },
      { name: 'probuilder-get-mesh-info', description: 'Retrieve faces, vertices, and edges data' },
      { name: 'probuilder-extrude', description: 'Extrude faces along their normals' },
      { name: 'probuilder-delete-faces', description: 'Remove faces to create holes or trim geometry' },
      { name: 'probuilder-set-face-material', description: 'Assign materials to individual faces' },
    ],
  },
  {
    name: 'Splines',
    description: 'AI-assisted Spline authoring: containers, knots, tangents, and evaluation.',
    packageId: 'com.ivanmurzak.unity.mcp.splines',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Splines.git',
    tools: [
      { name: 'splines-container-create', description: 'Create a SplineContainer in the scene' },
      { name: 'splines-add-knot', description: 'Append a knot to a spline' },
      { name: 'splines-set-knot', description: "Set a knot's position, tangents, and rotation" },
      { name: 'splines-set-tangent-mode', description: "Set a knot's tangent mode" },
      { name: 'splines-evaluate', description: 'Evaluate position/tangent/up along a spline' },
      { name: 'splines-modify', description: 'Modify any Splines component via ReflectorNet' },
    ],
  },
  {
    name: 'Terrain',
    description: 'AI-powered Unity Terrain authoring tools.',
    packageId: 'com.ivanmurzak.unity.mcp.terrain',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Terrain.git',
    tools: [
      { name: 'terrain-create', description: 'Create a Terrain GameObject backed by new TerrainData' },
      { name: 'terrain-set-heights', description: 'Sculpt heightmap values over a region or the whole terrain' },
      { name: 'terrain-paint-layer', description: 'Paint a TerrainLayer onto the alphamap (splatmap)' },
      { name: 'terrain-place-trees', description: 'Scatter or place trees from a tree prototype' },
      { name: 'terrain-set-neighbors', description: 'Stitch neighbor Terrains so Unity blends seams' },
    ],
  },
  {
    name: 'Tilemap',
    description: 'AI-assisted 2D Tilemap creation, painting, and tile/RuleTile asset tools.',
    packageId: 'com.ivanmurzak.unity.mcp.tilemap',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Tilemap.git',
    tools: [
      { name: 'tilemap-create', description: 'Create a Grid + Tilemap + TilemapRenderer' },
      { name: 'tilemap-set-tile', description: 'Paint a tile into a cell' },
      { name: 'tilemap-box-fill', description: 'Fill a rectangular region with a tile' },
      { name: 'tilemap-create-tile-asset', description: 'Create a Tile asset from a Sprite' },
      { name: 'tilemap-create-rule-tile', description: 'Create a RuleTile asset (2D Tilemap Extras)' },
    ],
  },
  {
    name: 'Timeline',
    description: 'AI-assisted Timeline cutscene and sequence authoring tools.',
    packageId: 'com.ivanmurzak.unity.mcp.timeline',
    version: null,
    gitUrl: 'https://github.com/IvanMurzak/Unity-AI-Timeline.git',
    tools: [
      { name: 'timeline-create', description: 'Create a TimelineAsset (.playable)' },
      { name: 'timeline-track-add', description: 'Add Animation/Activation/Audio/Signal/Control tracks' },
      { name: 'timeline-clip-add', description: 'Add clips to a track with start and duration' },
      { name: 'timeline-director-bind', description: 'Bind a TimelineAsset to a scene PlayableDirector' },
      { name: 'timeline-modify', description: 'Modify any Timeline object via ReflectorNet' },
    ],
  },
] as const;

/** True when a descriptor carries a concrete version pin (drives the up-to-date / update decision). */
export function hasVersion(descriptor: ExtensionDescriptor): boolean {
  return descriptor.version !== null && descriptor.version.trim() !== '';
}

/**
 * Resolve a user-supplied `<id>` to a catalogue descriptor. Matches by `packageId`
 * first (case-insensitively — UPM package names are lowercase by convention, and
 * this is the install identity), then falls back to an exact case-insensitive `name`
 * match for convenience (`unity-mcp-cli install-extension Tilemap`). Returns `null`
 * when absent or `id` is empty.
 */
export function findExtension(
  id: string | undefined | null,
  catalog: readonly ExtensionDescriptor[] = EXTENSIONS_CATALOG,
): ExtensionDescriptor | null {
  if (id === undefined || id === null) return null;
  const needle = id.trim();
  if (needle === '') return null;

  const byPackageId = catalog.find((d) => d.packageId.toLowerCase() === needle.toLowerCase());
  if (byPackageId) return byPackageId;

  return catalog.find((d) => d.name.toLowerCase() === needle.toLowerCase()) ?? null;
}
