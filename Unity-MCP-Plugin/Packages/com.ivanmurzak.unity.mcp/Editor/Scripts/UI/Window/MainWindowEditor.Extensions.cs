/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private static readonly ExtensionPanel.ExtensionData[] _extensions =
        {
            new(
                name:        "Animation",
                description: "AI-driven animation control and playback tools.",
                packageId:   "com.ivanmurzak.unity.mcp.animation",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md",
                tools: new[]
                {
                    ("animation-create",   "Create AnimationClip assets with keyframes"),
                    ("animation-get-data", "Inspect clip curves, events, and properties"),
                    ("animation-modify",   "Edit curves, events, and settings on a clip"),
                    ("animator-create",    "Create AnimatorController assets"),
                    ("animator-get-data",  "Inspect controller layers, states, and parameters"),
                    ("animator-modify",    "Edit parameters, states, and transitions"),
                }
            ),
            new(
                name:        "Cinemachine",
                description: "AI-assisted Cinemachine camera setup and configuration tools.",
                packageId:   "com.ivanmurzak.unity.mcp.cinemachine",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md",
                tools: new[]
                {
                    ("cinemachine-camera-create", "Create a CinemachineCamera in the scene"),
                    ("cinemachine-set-targets",   "Set the Follow and LookAt targets"),
                    ("cinemachine-set-lens",      "Configure FOV, clip planes, and dutch"),
                    ("cinemachine-set-body",      "Set the position-control component (Follow/Orbital/...)"),
                    ("cinemachine-set-noise",     "Add camera shake via Perlin noise"),
                }
            ),
            new(
                name:        "InputSystem",
                description: "AI-assisted Unity Input System authoring: InputActionAssets, maps, actions, bindings, and control schemes.",
                packageId:   "com.ivanmurzak.unity.mcp.inputsystem",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md",
                tools: new[]
                {
                    ("inputsystem-asset-create",          "Create a new .inputactions InputActionAsset"),
                    ("inputsystem-actionmap-add",         "Add an ActionMap to the asset"),
                    ("inputsystem-action-add",            "Add an Action (type + expectedControlType)"),
                    ("inputsystem-binding-add",           "Add a binding path to an Action"),
                    ("inputsystem-binding-composite-add", "Add a composite binding (2DVector/1DAxis)"),
                    ("inputsystem-controlscheme-add",     "Add a control scheme with device requirements"),
                    ("inputsystem-get",                   "Read the asset's maps, actions, and bindings"),
                }
            ),
            new(
                name:        "Navigation",
                description: "AI-driven NavMesh navigation: surfaces, baking, agents, and links.",
                packageId:   "com.ivanmurzak.unity.mcp.navigation",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md",
                tools: new[]
                {
                    ("navigation-surface-add",          "Add and configure a NavMeshSurface"),
                    ("navigation-set-bake-settings",    "Set agent radius/height/slope/step and voxel size"),
                    ("navigation-surface-bake",         "Bake or clear a NavMeshSurface"),
                    ("navigation-modifier-add",         "Add a NavMeshModifier (override area / ignore)"),
                    ("navigation-modifier-volume-add",  "Add a NavMeshModifierVolume"),
                    ("navigation-link-add",             "Add a NavMeshLink between two points"),
                    ("navigation-agent-add",            "Add and configure a NavMeshAgent"),
                    ("navigation-agent-set-destination","Set a NavMeshAgent's destination"),
                    ("navigation-list",                 "List NavMeshSurfaces and NavMeshAgents"),
                    ("navigation-get",                  "Serialize any NavMesh component"),
                    ("navigation-modify",               "Modify any NavMesh component via ReflectorNet"),
                }
            ),
            new(
                name:        "ParticleSystem",
                description: "AI-powered particle system creation and control tools.",
                packageId:   "com.ivanmurzak.unity.mcp.particlesystem",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md",
                tools: new[]
                {
                    ("particle-system-get",    "Inspect ParticleSystem modules and settings"),
                    ("particle-system-modify", "Modify emission, shape, color, noise, and more"),
                }
            ),
            new(
                name:        "ProBuilder",
                description: "AI-assisted ProBuilder geometry modeling tools.",
                packageId:   "com.ivanmurzak.unity.mcp.probuilder",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md",
                tools: new[]
                {
                    ("probuilder-create-shape",     "Create editable 3D primitives in the scene"),
                    ("probuilder-get-mesh-info",     "Retrieve faces, vertices, and edges data"),
                    ("probuilder-extrude",           "Extrude faces along their normals"),
                    ("probuilder-delete-faces",      "Remove faces to create holes or trim geometry"),
                    ("probuilder-set-face-material", "Assign materials to individual faces"),
                }
            ),
            new(
                name:        "Splines",
                description: "AI-assisted Spline authoring: containers, knots, tangents, and evaluation.",
                packageId:   "com.ivanmurzak.unity.mcp.splines",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md",
                tools: new[]
                {
                    ("splines-container-create", "Create a SplineContainer in the scene"),
                    ("splines-add-knot",         "Append a knot to a spline"),
                    ("splines-set-knot",         "Set a knot's position, tangents, and rotation"),
                    ("splines-set-tangent-mode", "Set a knot's tangent mode"),
                    ("splines-evaluate",         "Evaluate position/tangent/up along a spline"),
                    ("splines-modify",           "Modify any Splines component via ReflectorNet"),
                }
            ),
            new(
                name:        "Terrain",
                description: "AI-powered Unity Terrain authoring tools.",
                packageId:   "com.ivanmurzak.unity.mcp.terrain",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md",
                tools: new[]
                {
                    ("terrain-create",       "Create a Terrain GameObject backed by new TerrainData"),
                    ("terrain-set-heights",  "Sculpt heightmap values over a region or the whole terrain"),
                    ("terrain-paint-layer",  "Paint a TerrainLayer onto the alphamap (splatmap)"),
                    ("terrain-place-trees",  "Scatter or place trees from a tree prototype"),
                    ("terrain-set-neighbors", "Stitch neighbor Terrains so Unity blends seams"),
                }
            ),
            new(
                name:        "Tilemap",
                description: "AI-assisted 2D Tilemap creation, painting, and tile/RuleTile asset tools.",
                packageId:   "com.ivanmurzak.unity.mcp.tilemap",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md",
                tools: new[]
                {
                    ("tilemap-create",            "Create a Grid + Tilemap + TilemapRenderer"),
                    ("tilemap-set-tile",          "Paint a tile into a cell"),
                    ("tilemap-box-fill",          "Fill a rectangular region with a tile"),
                    ("tilemap-create-tile-asset", "Create a Tile asset from a Sprite"),
                    ("tilemap-create-rule-tile",  "Create a RuleTile asset (2D Tilemap Extras)"),
                }
            ),
            new(
                name:        "Timeline",
                description: "AI-assisted Timeline cutscene and sequence authoring tools.",
                packageId:   "com.ivanmurzak.unity.mcp.timeline",
                gitUrl:      "https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md",
                tools: new[]
                {
                    ("timeline-create",         "Create a TimelineAsset (.playable)"),
                    ("timeline-track-add",      "Add Animation/Activation/Audio/Signal/Control tracks"),
                    ("timeline-clip-add",       "Add clips to a track with start and duration"),
                    ("timeline-director-bind",  "Bind a TimelineAsset to a scene PlayableDirector"),
                    ("timeline-modify",         "Modify any Timeline object via ReflectorNet"),
                }
            ),
        };

        internal static IReadOnlyList<ExtensionPanel.ExtensionData> ExtensionCatalog => _extensions;

        private void SetupExtensionsSection(VisualElement root)
        {
            var container = root.Q<VisualElement>("ExtensionsSection");
            if (container == null)
                return;

            var installAll = root.Q<Button>("btnExtensionsInstallAll");
            var updateAll = root.Q<Button>("btnExtensionsUpdateAll");

            var panels = new List<ExtensionPanel>(_extensions.Length);
            foreach (var extension in _extensions)
            {
                var panel = new ExtensionPanel(extension);
                container.Add(panel.Root);
                panels.Add(panel);
                panel.StateChanged += RefreshBulkButtons;
            }

            installAll.clicked += () => _ = ExtensionPanel.InstallOrUpdateAsync(panels.Where(p => p.CanInstall).ToList());
            updateAll.clicked += () => _ = ExtensionPanel.InstallOrUpdateAsync(panels.Where(p => p.CanBulkUpdate).ToList());
            RefreshBulkButtons();

            void RefreshBulkButtons()
            {
                var installable = panels.Count(p => p.CanInstall);
                var updatable = panels.Count(p => p.CanBulkUpdate);

                installAll.SetEnabled(installable > 0);
                installAll.tooltip = installable > 0
                    ? $"Install {installable} extension(s) that are not installed yet."
                    : "Nothing to install right now.";

                updateAll.style.display = updatable > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                updateAll.tooltip = $"Update {updatable} installed extension(s) to the latest version.";
            }

            _ = RefreshPanelsAsync(panels);
        }

        private static async Task RefreshPanelsAsync(IReadOnlyList<ExtensionPanel> panels)
        {
            Dictionary<string, PackageInfo> installed;
            try
            {
                installed = await PackageUtils.ListInstalledAsync();
            }
            catch (Exception ex)
            {
                // Stay on "Checking...": treating this as "nothing installed" would offer Install All over real installs.
                Debug.LogWarning($"[Unity-MCP] Failed to read the installed extensions: {ex.Message}");
                return;
            }

            for (var i = 0; i < _extensions.Length; i++)
            {
                var isInstalled = installed.TryGetValue(_extensions[i].PackageId, out var package);
                panels[i].RefreshStatus(isInstalled ? package.version : null, isInstalled && package.source == PackageSource.Registry);
            }
        }
    }
}
