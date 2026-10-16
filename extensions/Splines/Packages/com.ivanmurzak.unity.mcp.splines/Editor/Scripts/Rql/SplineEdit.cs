/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>
    /// Shared checks and the undo / dirty bookkeeping for spline edits. Writes through <see cref="SplineView"/> /
    /// <see cref="KnotView"/> have a plain object as RQL owner (the engine records only UnityEngine.Object owners), so they
    /// record their SplineContainer themselves; the Undo entry joins the mutation's Undo group.
    /// </summary>
    internal static class SplineEdit
    {
        public static Spline GetSpline(SplineContainer container, int splineIndex)
        {
            var count = container.Splines.Count;
            if (splineIndex < 0 || splineIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(splineIndex), $"spline index {splineIndex} is out of range: the container has {count} spline(s)");
            return container.Splines[splineIndex];
        }

        public static void CheckKnot(Spline spline, int knotIndex, bool allowEnd = false)
        {
            var max = allowEnd ? spline.Count : spline.Count - 1;
            if (knotIndex < 0 || knotIndex > max)
                throw new ArgumentOutOfRangeException(nameof(knotIndex), $"knot index {knotIndex} is out of range: the spline has {spline.Count} knot(s)");
        }

        public static void Record(SplineContainer container, string what) => Undo.RecordObject(container, "RQL " + what);

        public static void Dirty(SplineContainer container)
        {
            EditorUtility.SetDirty(container);
            var scene = container.gameObject.scene;
            if (scene.IsValid() && !EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Record, change, mark dirty.</summary>
        public static void Edit(SplineContainer container, string what, Action change)
        {
            Record(container, what);
            change();
            Dirty(container);
        }
    }
}
