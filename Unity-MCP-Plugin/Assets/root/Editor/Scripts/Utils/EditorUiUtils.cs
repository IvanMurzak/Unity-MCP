/*
Copyright (c) 2025 Ivan Murzak
Licensed under the MIT License.
See the LICENSE file in the project root for more information.
*/

#nullable enable
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Shared UI Toolkit helpers for editor tooling.
    /// </summary>
    public static class EditorUiUtils
    {
        const float ColumnResizerWidth = 4f;
        static readonly Color ColumnResizerColor = new Color(0f, 0f, 0f, 0.2f);

        /// <summary>
        /// Creates a standard divider element.
        /// </summary>
        public static VisualElement BuildDivider()
        {
            var divider = new VisualElement();
            divider.AddToClassList("divider");
            return divider;
        }

        /// <summary>
        /// Applies collapsed/expanded classes for a foldout.
        /// </summary>
        /// <param name="foldout">Foldout to update.</param>
        /// <param name="isExpanded">Whether the foldout is expanded.</param>
        public static void UpdateFoldoutState(Foldout foldout, bool isExpanded)
        {
            if (isExpanded)
                foldout.RemoveFromClassList("collapsed");
            else
                foldout.AddToClassList("collapsed");
        }

        /// <summary>
        /// Applies header styling to a foldout label.
        /// </summary>
        /// <param name="foldout">Foldout to style.</param>
        public static void ApplyFoldoutHeaderStyle(Foldout foldout)
        {
            ApplyFoldoutLabelClass(foldout, "header");
        }

        /// <summary>
        /// Adds a label class to the foldout text, retrying once if the label isn't ready.
        /// </summary>
        /// <param name="foldout">Foldout to style.</param>
        /// <param name="className">USS class name to add.</param>
        public static void ApplyFoldoutLabelClass(Foldout foldout, string className)
        {
            if (!TryApplyFoldoutLabelClass(foldout, className))
                foldout.schedule.Execute(() => TryApplyFoldoutLabelClass(foldout, className));
        }

        /// <summary>
        /// Prevents nested foldout toggle events from collapsing the parent foldout.
        /// </summary>
        /// <param name="foldout">Foldout to isolate.</param>
        public static void StopFoldoutTogglePropagation(Foldout foldout)
        {
            foldout.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            foldout.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            foldout.RegisterCallback<ChangeEvent<bool>>(evt => evt.StopPropagation());
        }

        /// <summary>
        /// Adds toggle classes and wiring to keep the style in sync with state.
        /// </summary>
        /// <param name="toggle">Toggle to style.</param>
        public static void RegisterToggleClass(Toggle toggle)
        {
            toggle.AddToClassList("item-switch");
            toggle.RegisterValueChangedCallback(evt => UpdateToggleClass(toggle, evt.newValue));
        }

        /// <summary>
        /// Updates toggle classes for checked state.
        /// </summary>
        /// <param name="toggle">Toggle to update.</param>
        /// <param name="isOn">Whether the toggle is on.</param>
        public static void UpdateToggleClass(Toggle toggle, bool isOn)
        {
            if (isOn)
                toggle.AddToClassList("checked");
            else
                toggle.RemoveFromClassList("checked");
        }

        /// <summary>
        /// Builds a draggable column resizer element.
        /// </summary>
        /// <param name="getWidth">Gets the current width.</param>
        /// <param name="getMinWidth">Gets the minimum width.</param>
        /// <param name="setWidth">Applies the new width.</param>
        public static VisualElement BuildColumnResizer(
            System.Func<float> getWidth,
            System.Func<float> getMinWidth,
            System.Action<float> setWidth)
        {
            var resizer = new VisualElement();
            resizer.style.width = ColumnResizerWidth;
            resizer.style.height = Length.Percent(100);
            resizer.style.flexShrink = 0f;
            resizer.style.backgroundColor = ColumnResizerColor;
            RegisterColumnResizer(resizer, getWidth, getMinWidth, setWidth);
            return resizer;
        }

        /// <summary>
        /// Creates a standard table cell label with truncation and header styling.
        /// </summary>
        /// <param name="text">Label text.</param>
        /// <param name="isHeader">Whether this is a header label.</param>
        public static Label CreateCellLabel(string text, bool isHeader)
        {
            var label = new Label(text);
            label.style.fontSize = isHeader ? 11 : 10;
            label.style.unityFontStyleAndWeight = isHeader ? FontStyle.Bold : FontStyle.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.flexGrow = 1f;
            return label;
        }

        /// <summary>
        /// Removes all children at and after a given index.
        /// </summary>
        /// <param name="list">Container element.</param>
        /// <param name="startIndex">Index to start removing from.</param>
        public static void ClearChildrenFromIndex(VisualElement list, int startIndex)
        {
            if (startIndex < 0)
                startIndex = 0;

            if (list.childCount <= startIndex)
                return;

            while (list.childCount > startIndex)
                list.RemoveAt(startIndex);
        }

        /// <summary>
        /// Adds a scroll wheel handler that supports horizontal scrolling with shift.
        /// </summary>
        /// <param name="scroll">Scroll view to update.</param>
        /// <param name="rootScroll">Optional outer scroll view.</param>
        /// <param name="shiftWheelHorizontalThreshold">Threshold for horizontal delta on shift.</param>
        public static void ConfigureHistoryScrollWheel(
            ScrollView scroll,
            ScrollView? rootScroll,
            float shiftWheelHorizontalThreshold)
        {
            var threshold = Mathf.Max(0f, shiftWheelHorizontalThreshold);
            scroll.RegisterCallback<WheelEvent>(evt =>
            {
                var offset = scroll.scrollOffset;
                var wheelDelta = evt.delta;
                if (evt.shiftKey)
                {
                    var appliedDelta = Mathf.Abs(wheelDelta.x) >= threshold
                        ? wheelDelta.x
                        : wheelDelta.y;
                    var scaledDelta = appliedDelta * scroll.mouseWheelScrollSize;
                    TryScrollHorizontal(scroll, offset, scaledDelta);
                    evt.StopImmediatePropagation();
                    return;
                }

                var scaledDeltaY = wheelDelta.y * scroll.mouseWheelScrollSize;
                if (TryScrollVertical(scroll, offset, scaledDeltaY))
                {
                    evt.StopImmediatePropagation();
                    return;
                }

                if (rootScroll != null && rootScroll != scroll)
                {
                    var rootScaledDeltaY = wheelDelta.y * rootScroll.mouseWheelScrollSize;
                    if (TryScrollVertical(rootScroll, rootScroll.scrollOffset, rootScaledDeltaY))
                    {
                        evt.StopImmediatePropagation();
                        return;
                    }
                }

                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
        }

        static bool TryApplyFoldoutLabelClass(Foldout foldout, string className)
        {
            var label = foldout.Q<Label>(className: "unity-foldout__text");
            if (label == null)
                return false;

            label.AddToClassList(className);
            return true;
        }

        static void RegisterColumnResizer(
            VisualElement resizer,
            System.Func<float> getWidth,
            System.Func<float> getMinWidth,
            System.Action<float> setWidth)
        {
            var startWidth = 0f;
            var startX = 0f;
            var dragging = false;

            resizer.RegisterCallback<PointerDownEvent>(evt =>
            {
                dragging = true;
                startWidth = getWidth();
                startX = evt.position.x;
                resizer.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            resizer.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging || !resizer.HasPointerCapture(evt.pointerId))
                    return;

                var delta = evt.position.x - startX;
                var nextWidth = Mathf.Max(getMinWidth(), startWidth + delta);
                setWidth(nextWidth);
                evt.StopPropagation();
            });

            resizer.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging || !resizer.HasPointerCapture(evt.pointerId))
                    return;

                dragging = false;
                resizer.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }

        static bool TryScrollVertical(ScrollView scroll, Vector2 offset, float deltaY)
        {
            var maxY = Mathf.Max(0f, scroll.contentContainer.layout.height - scroll.contentViewport.layout.height);
            if (maxY <= 0f)
                return false;

            var nextY = Mathf.Clamp(offset.y + deltaY, 0f, maxY);
            if (Mathf.Approximately(nextY, offset.y))
                return false;

            scroll.scrollOffset = new Vector2(offset.x, nextY);
            return true;
        }

        static bool TryScrollHorizontal(ScrollView scroll, Vector2 offset, float deltaX)
        {
            var maxX = Mathf.Max(0f, scroll.contentContainer.layout.width - scroll.contentViewport.layout.width);
            if (maxX <= 0f)
                return false;

            var nextX = Mathf.Clamp(offset.x + deltaX, 0f, maxX);
            if (Mathf.Approximately(nextX, offset.x))
                return false;

            scroll.scrollOffset = new Vector2(nextX, offset.y);
            return true;
        }
    }
}
#endif
