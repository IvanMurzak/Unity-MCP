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
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-Animation.git",
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-Cinemachine.git",
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-InputSystem.git",
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
                name:        "ParticleSystem",
                description: "AI-powered particle system creation and control tools.",
                packageId:   "com.ivanmurzak.unity.mcp.particlesystem",
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-ParticleSystem.git",
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-ProBuilder.git",
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-Splines.git",
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
                gitUrl:      "https://github.com/IvanMurzak/Unity-AI-Terrain.git",
                tools: new[]
                {
                    ("terrain-create",       "Create a Terrain GameObject backed by new TerrainData"),
                    ("terrain-set-heights",  "Sculpt heightmap values over a region or the whole terrain"),
                    ("terrain-paint-layer",  "Paint a TerrainLayer onto the alphamap (splatmap)"),
                    ("terrain-place-trees",  "Scatter or place trees from a tree prototype"),
                    ("terrain-set-neighbors", "Stitch neighbor Terrains so Unity blends seams"),
                }
            ),
        };

        private void SetupExtensionsSection(VisualElement root)
        {
            var container = root.Q<VisualElement>("ExtensionsSection");
            if (container == null)
                return;

            var panels = new List<ExtensionPanel>(_extensions.Length);
            foreach (var extension in _extensions)
            {
                var panel = new ExtensionPanel(extension);
                container.Add(panel.Root);
                panels.Add(panel);
            }

            var listRequest = Client.List();
            EditorApplication.update += OnListComplete;

            void OnListComplete()
            {
                if (!listRequest.IsCompleted)
                    return;

                EditorApplication.update -= OnListComplete;

                Dictionary<string, PackageInfo> installedByName;
                if (listRequest.Status == StatusCode.Success)
                    installedByName = listRequest.Result.ToDictionary(p => p.name, p => p);
                else
                    installedByName = new Dictionary<string, PackageInfo>();

                for (var i = 0; i < _extensions.Length; i++)
                {
                    var packageId = _extensions[i].PackageId;
                    var installedVersion = installedByName.TryGetValue(packageId, out var pkg)
                        ? pkg.version
                        : null;

                    panels[i].RefreshStatus(installedVersion);
                }
            }
        }
    }
}
