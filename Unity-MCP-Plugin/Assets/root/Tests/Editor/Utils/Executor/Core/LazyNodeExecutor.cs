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
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>
    /// Lazy pipeline node that passes the result through the tree.
    /// Accepts Action<object?> or Func<object?, object?>.
    /// </summary>
    public partial class LazyNodeExecutor
    {
        private readonly LinkedList<Func<object?, object?>> operations = new();

        // Sibling nodes (horizontal) — executed after the current node (sequentially).
        private readonly List<LazyNodeExecutor> _next = new();

        // Child nodes (vertical).
        private readonly List<LazyNodeExecutor> _children = new();

        // Dependency nodes (horizontal, before).
        private readonly List<LazyNodeExecutor> _dependencies = new();

        // ---- Execution ----

        /// <summary>
        /// Execute the tree. The result of each step is passed to the next according to the traversal order.
        /// </summary>
        public object? Execute(object? input = null, TraversalOrder order = TraversalOrder.DepthFirst)
        {
            try
            {
                var result = order switch
                {
                    TraversalOrder.DepthFirst => ExecuteDepthFirst(input),
                    TraversalOrder.BreadthFirst => ExecuteBreadthFirst(input),
                    _ => throw new ArgumentOutOfRangeException(nameof(order))
                };
                return result;
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (InconclusiveException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                UnityEngine.Debug.LogError($"Error executing LazyNode: {ex}");
                throw;
            }
        }

        protected virtual void PostExecute(object? input = null) { }

        // DFS: current -> all children (including each child's siblings) -> all siblings of the current node
        private object? ExecuteDepthFirst(object? input)
        {
            if (operations == null)
                throw new InvalidOperationException("No action set for this LazyNode.");

            object? result = input;

            foreach (var dependency in _dependencies)
            {
                result = dependency.ExecuteDepthFirst(result);
            }

            try
            {
                foreach (var operation in operations)
                {
                    result = operation(input);
                }

                // First, each "root" of the child subtree along with its own siblings
                foreach (var childHead in _children)
                {
                    foreach (var nodeInChain in EnumerateChain(childHead))
                    {
                        result = nodeInChain.ExecuteDepthFirst(result);
                    }
                }
            }
            finally
            {
                PostExecute(input);
            }

            // Then the current node's siblings
            foreach (var sibling in _next)
            {
                result = sibling.ExecuteDepthFirst(result);
            }

            return result;
        }

        // BFS: level by level. At each level, process all "siblings",
        // then collect the next level from children (including their siblings).
        private object? ExecuteBreadthFirst(object? input)
        {
            object? result = input;

            foreach (var dependency in _dependencies)
            {
                result = dependency.ExecuteBreadthFirst(result);
            }

            try
            {
                // Current level: the current node and its chain of siblings
                var level = new List<LazyNodeExecutor>(EnumerateChain(this));

                while (level.Count > 0)
                {
                    // Execute all nodes of the level sequentially, passing the result along
                    foreach (var node in level)
                    {
                        if (node.operations == null)
                            throw new InvalidOperationException("No action set for this LazyNode.");

                        foreach (var operation in node.operations)
                        {
                            result = operation(input);
                        }
                    }

                    // Build the next level: for each node of the level — all its children and their siblings
                    var nextLevel = new List<LazyNodeExecutor>();
                    foreach (var node in level)
                    {
                        foreach (var childHead in node._children)
                        {
                            nextLevel.AddRange(EnumerateChain(childHead));
                        }
                    }

                    level = nextLevel;
                }
            }
            finally
            {
                PostExecute(input);
            }

            return result;
        }

        // Iterate over the "chain" of siblings starting from the head
        private static IEnumerable<LazyNodeExecutor> EnumerateChain(LazyNodeExecutor head)
        {
            yield return head;
            foreach (var n in head._next)
                yield return n;
        }
    }

    public enum TraversalOrder
    {
        DepthFirst,  // DFS
        BreadthFirst // BFS
    }
}
