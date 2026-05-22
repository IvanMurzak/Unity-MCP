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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI.Controls
{
    /// <summary>
    /// A segmented control with N segments and a sliding highlight.
    /// Implements <see cref="INotifyValueChanged{T}"/> for UI Toolkit integration.
    /// All visual styling is defined in <c>_segmented-control.uss</c>.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SegmentedControl : VisualElement, INotifyValueChanged<int>
#else
    public class SegmentedControl : VisualElement, INotifyValueChanged<int>
#endif
    {
        private static readonly string[] UssPaths =
            EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/common/_segmented-control.uss");

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("choices")]
        public string Choices
        {
            get => string.Join(",", _segmentLabels);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var labels = value.Split(',');
                    for (var i = 0; i < labels.Length; i++)
                        labels[i] = labels[i].Trim();
                    _segmentLabels = labels;
                    BuildSegments(labels);
                }
            }
        }
        private string[] _segmentLabels = Array.Empty<string>();

        [UxmlAttribute("default-index")]
        public int DefaultIndex
        {
            get => _selectedIndex;
            set => SetValueWithoutNotify(value);
        }
#else
        public new class UxmlFactory : UxmlFactory<SegmentedControl, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _choices = new() { name = "choices", defaultValue = "" };
            private readonly UxmlIntAttributeDescription _defaultIndex = new() { name = "default-index", defaultValue = 0 };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SegmentedControl)ve;
                var choicesValue = _choices.GetValueFromBag(bag, cc);
                var defaultIndex = _defaultIndex.GetValueFromBag(bag, cc);

                if (!string.IsNullOrEmpty(choicesValue))
                {
                    var labels = choicesValue.Split(',');
                    for (var i = 0; i < labels.Length; i++)
                        labels[i] = labels[i].Trim();
                    control.BuildSegments(labels);
                    control.SetValueWithoutNotify(defaultIndex);
                }
            }
        }
#endif

        private readonly VisualElement _track;
        private readonly VisualElement _highlight;
        private readonly List<Label> _segments = new();
        private int _selectedIndex;
        private bool _transitionReady;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetValue(value);
        }

        int INotifyValueChanged<int>.value
        {
            get => _selectedIndex;
            set => SetValue(value);
        }

        public SegmentedControl() : this(Array.Empty<string>()) { }

        public SegmentedControl(params string[] labels)
        {
            AddToClassList("segmented-control");

            var uss = EditorAssetLoader.LoadAssetAtPath<StyleSheet>(UssPaths);
            if (uss != null)
                styleSheets.Add(uss);

            _track = new VisualElement();
            _track.AddToClassList("segmented-control__track");
            Add(_track);

            _highlight = new VisualElement();
            _highlight.AddToClassList("segmented-control__highlight");
            _highlight.AddToClassList("segmented-control__highlight--no-transition");
            _track.Add(_highlight);

            if (labels.Length > 0)
                BuildSegments(labels);
        }

        private void BuildSegments(string[] labels)
        {
#if UNITY_2023_2_OR_NEWER
            _segmentLabels = labels;
#endif
            // Remove old segments (keep highlight at index 0)
            foreach (var seg in _segments)
                _track.Remove(seg);
            _segments.Clear();

            for (var i = 0; i < labels.Length; i++)
            {
                var label = new Label(labels[i]);
                label.AddToClassList("segmented-control__segment");
                var index = i;
                label.RegisterCallback<ClickEvent>(_ => SelectedIndex = index);
                label.RegisterCallback<GeometryChangedEvent>(_ => SnapHighlightToSegment());
                _track.Add(label);
                _segments.Add(label);
            }

            _transitionReady = false;
            UpdateHighlight();
        }

        /// <summary>
        /// Sets per-segment tooltips. Segment count must match.
        /// </summary>
        public void SetTooltips(params string[] tooltips)
        {
            for (var i = 0; i < tooltips.Length && i < _segments.Count; i++)
                _segments[i].tooltip = tooltips[i];
        }

        public void SetValueWithoutNotify(int newValue)
        {
            if (newValue < 0 || (_segments.Count > 0 && newValue >= _segments.Count))
                return;

            _selectedIndex = newValue;
            UpdateHighlight();
        }

        private void SetValue(int newValue)
        {
            if (newValue < 0 || (_segments.Count > 0 && newValue >= _segments.Count))
                return;
            if (newValue == _selectedIndex)
                return;

            using var evt = ChangeEvent<int>.GetPooled(_selectedIndex, newValue);
            evt.target = this;
            _selectedIndex = newValue;
            UpdateHighlight();
            SendEvent(evt);
        }

        /// <summary>
        /// Positions the highlight to match the selected segment's actual layout.
        /// Called on geometry changes and selection changes.
        /// </summary>
        private void SnapHighlightToSegment()
        {
            if (_segments.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _segments.Count)
                return;

            var segment = _segments[_selectedIndex];
            var x = segment.layout.x;
            var w = segment.layout.width;

            // layout is zero before first layout pass
            if (float.IsNaN(w) || w <= 0)
                return;

            _highlight.style.left = x;
            _highlight.style.width = w;
        }

        private void UpdateHighlight()
        {
            if (_segments.Count == 0)
                return;

            if (!_transitionReady)
            {
                // Start with transitions suppressed; enable after first layout
                SnapHighlightToSegment();
                schedule.Execute(() =>
                {
                    _transitionReady = true;
                    _highlight.RemoveFromClassList("segmented-control__highlight--no-transition");
                });
            }
            else
            {
                SnapHighlightToSegment();
            }

            for (var i = 0; i < _segments.Count; i++)
                _segments[i].EnableInClassList("segmented-control__segment--selected", i == _selectedIndex);
        }
    }
}
