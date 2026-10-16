/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputSys = UnityEngine.InputSystem.InputSystem;

    /// <summary>
    /// The <c>input</c> source: the project's InputActionAssets, the names bindings may use (layouts, interactions,
    /// processors) and CreateAsset. Everything inside an asset is edited through the asset itself
    /// (<see cref="UnityEngine_InputSystem_InputActionAsset_Adapter"/>).
    /// </summary>
    [RqlDescription("Input System: assets (every .inputactions asset), layouts, interactions, processors; call CreateAsset(path, map?)")]
    public sealed class InputEditorApi
    {
        public static readonly InputEditorApi Instance = new InputEditorApi();

        [RqlDescription("every InputActionAsset of the project (at most 200)")]
        public List<InputActionAsset> assets => AssetDatabase.FindAssets("t:InputActionAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .Take(RqlUnity.MaxAssets)
            .Select(AssetDatabase.LoadAssetAtPath<InputActionAsset>)
            .Where(a => a != null)
            .ToList();

        [RqlDescription("registered control layouts: device layouts for paths (\"Gamepad\" → \"<Gamepad>/…\") and control types (\"Vector2\")")]
        public List<string> layouts => InputSys.ListLayouts().OrderBy(n => n).ToList();

        [RqlDescription("registered interactions (\"hold\", \"tap\", \"press\" …)")]
        public List<string> interactions => InputSys.ListInteractions().OrderBy(n => n).ToList();

        [RqlDescription("registered processors (\"invert\", \"normalize\", \"stickDeadzone\" …)")]
        public List<string> processors => InputSys.ListProcessors().OrderBy(n => n).ToList();

        [RqlCallable("create an .inputactions asset at path (\"Assets/…/Player.inputactions\", missing folders are created) with an optional " +
                     "first action map; returns the asset")]
        public InputActionAsset CreateAsset(string path, string? map = null) => InputAssetStore.Create(path, map);
    }
}
