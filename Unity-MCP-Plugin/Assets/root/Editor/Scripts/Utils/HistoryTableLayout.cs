/*
Copyright (c) 2025 Ivan Murzak
Licensed under the MIT License.
See the LICENSE file in the project root for more information.
*/

#nullable enable
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Tracks header and row cells for a resizable history table.
    /// </summary>
    /// <typeparam name="TColumn">Column identifier type.</typeparam>
    public sealed class HistoryTableLayout<TColumn> where TColumn : notnull
    {
        readonly Dictionary<TColumn, List<VisualElement>> headerCells = new();
        readonly Dictionary<TColumn, List<VisualElement>> rowCells = new();

        /// <summary>
        /// Clears all cached header and row cells.
        /// </summary>
        public void Reset()
        {
            headerCells.Clear();
            rowCells.Clear();
        }

        /// <summary>
        /// Clears cached row cells, preserving headers.
        /// </summary>
        public void ResetRowCells()
        {
            rowCells.Clear();
        }

        /// <summary>
        /// Creates a fixed-width cell and registers it for layout updates.
        /// </summary>
        /// <param name="column">Column identifier.</param>
        /// <param name="width">Fixed width.</param>
        /// <param name="content">Optional content element.</param>
        /// <param name="isHeader">Whether this is a header cell.</param>
        public VisualElement CreateFixedCell(
            TColumn column,
            float width,
            VisualElement? content = null,
            bool isHeader = false)
        {
            var cell = new VisualElement();
            cell.style.width = width;
            cell.style.minWidth = width;
            cell.style.maxWidth = width;
            cell.style.flexGrow = 0f;
            cell.style.flexShrink = 0f;
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            cell.style.overflow = Overflow.Hidden;

            if (content != null)
                cell.Add(content);

            RegisterCell(isHeader, column, cell);
            return cell;
        }

        /// <summary>
        /// Creates a flexible-width cell and registers it for layout updates.
        /// </summary>
        /// <param name="column">Column identifier.</param>
        /// <param name="minWidth">Minimum width.</param>
        /// <param name="grow">Flex grow value.</param>
        /// <param name="content">Optional content element.</param>
        /// <param name="isHeader">Whether this is a header cell.</param>
        public VisualElement CreateFlexCell(
            TColumn column,
            float minWidth,
            float grow,
            VisualElement? content = null,
            bool isHeader = false)
        {
            var cell = new VisualElement();
            cell.style.minWidth = minWidth;
            cell.style.flexBasis = minWidth;
            cell.style.flexGrow = grow;
            cell.style.flexShrink = 1f;
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            cell.style.overflow = Overflow.Hidden;

            if (content != null)
                cell.Add(content);

            RegisterCell(isHeader, column, cell);
            return cell;
        }

        /// <summary>
        /// Applies a new width to all registered cells in a column.
        /// </summary>
        /// <param name="column">Column identifier.</param>
        /// <param name="width">Width to apply.</param>
        /// <param name="isFlexible">Whether this column is flexible.</param>
        public void ApplyColumnWidth(TColumn column, float width, bool isFlexible)
        {
            ApplyColumnWidth(headerCells, column, width, isFlexible);
            ApplyColumnWidth(rowCells, column, width, isFlexible);
        }

        void RegisterCell(bool isHeader, TColumn column, VisualElement cell)
        {
            var map = isHeader ? headerCells : rowCells;
            if (!map.TryGetValue(column, out var list))
            {
                list = new List<VisualElement>();
                map[column] = list;
            }

            list.Add(cell);
        }

        static void ApplyColumnWidth(
            Dictionary<TColumn, List<VisualElement>> map,
            TColumn column,
            float width,
            bool isFlexible)
        {
            if (!map.TryGetValue(column, out var cells))
                return;

            foreach (var cell in cells)
            {
                if (isFlexible)
                {
                    cell.style.minWidth = width;
                    cell.style.flexBasis = width;
                }
                else
                {
                    cell.style.width = width;
                    cell.style.minWidth = width;
                    cell.style.maxWidth = width;
                }
            }
        }
    }
}
#endif
