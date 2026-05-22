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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectComponentDestroyToolId = "gameobject-component-destroy";
        [McpPluginTool
        (
            GameObjectComponentDestroyToolId,
            Title = "GameObject / Component / Destroy",
            DestructiveHint = true
        )]
        [McpPluginSkillDescription("Destroy one or more Components from a target GameObject. Missing (null) components " +
            "are skipped — they cannot be destroyed. " +
            "Use '" + GameObjectFindToolId + "' and '" + GameObjectComponentGetToolId +
            "' to identify the components first.")]
        [McpPluginSkillBody("Destroy one or many components from target GameObject. Can't destroy missed components. " +
            "Use '" + GameObjectFindToolId + "' tool to find the target GameObject and '" + GameObjectComponentGetToolId + "' to get component details first.\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the host GameObject.\n" +
            "- `destroyComponentRefs` — `ComponentRefList` of components to destroy (matched against the GameObject's components).\n\n" +
            "## Behavior\n\n" +
            "Iterates `go.GetComponents<Component>()`, skipping null entries (missing scripts). For each non-null component " +
            "that matches one of `destroyComponentRefs`, the tool snapshots a `ComponentRef`, calls `Object.DestroyImmediate`, " +
            "and records the destroyed reference. If no component matches at all, throws with the help text from " +
            "`Error.NotFoundComponents` (which includes a preview of all available components on the GameObject).")]
        [Description("Destroy one or many components from target GameObject. Can't destroy missed components. " +
            "Use '" + GameObjectFindToolId + "' tool to find the target GameObject and '" + GameObjectComponentGetToolId + "' to get component details first.")]
        public DestroyComponentsResponse DestroyComponents
        (
            GameObjectRef gameObjectRef,
            ComponentRefList destroyComponentRefs
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef));

            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            if (destroyComponentRefs == null)
                throw new ArgumentNullException(nameof(destroyComponentRefs));

            if (destroyComponentRefs.Count == 0)
                throw new ArgumentException("No components provided to destroy.", nameof(destroyComponentRefs));

            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception($"GameObject by {nameof(gameObjectRef)} not found.");

                var destroyCounter = 0;

                var allComponents = go.GetComponents<UnityEngine.Component>();

                var response = new DestroyComponentsResponse();

                foreach (var component in allComponents)
                {
                    if (component == null)
                        continue; // Skip null/missing script components

                    if (destroyComponentRefs.Any(cr => cr.Matches(component)))
                    {
                        var destroyedComponentRef = new ComponentRef(component);
                        UnityEngine.Object.DestroyImmediate(component);
                        destroyCounter++;
                        response.DestroyedComponents ??= new ComponentRefList();
                        response.DestroyedComponents.Add(destroyedComponentRef);
                    }
                }

                if (destroyCounter == 0)
                    throw new Exception(Error.NotFoundComponents(destroyComponentRefs, allComponents));

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

    }
}
