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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>
    /// Lazy pipeline node that passes the result through the tree.
    /// Accepts Action<object?> or Func<object?, object?>.
    /// </summary>
    public partial class LazyNodeExecutor
    {
        // Convenience factories
        public static LazyNodeExecutor FromAction<TInput>(Action<TInput> action) => new LazyNodeExecutor().SetAction(action);
        public static LazyNodeExecutor FromFunc<TInput, TResult>(Func<TInput, TResult> func) => new LazyNodeExecutor().SetAction(func);

        // ---- Constructors ----
        public LazyNodeExecutor SetAction(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // pass input through as output
            operations.AddLast(input => { action(); return input; });
            return this;
        }
        public LazyNodeExecutor SetAction<T>(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Action doesn't produce a result — pass input through as output
            operations.AddLast(input =>
            {
                if (input == null)
                {
                    action(default!);
                }
                else
                {
                    action((T)input);
                }
                return input;
            });
            return this;
        }

        public LazyNodeExecutor SetAction<TResult>(Func<TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            operations.AddLast(input => func());
            return this;
        }

        public LazyNodeExecutor SetAction<TInput, TResult>(Func<TInput, TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            operations.AddLast(input =>
            {
                if (input == null)
                {
                    return func(default!);
                }
                else
                {
                    return func((TInput)input);
                }
            });
            return this;
        }

        // ---- Tree construction (AddNext) ----

        /// <summary>Add a sibling node (horizontal, sequential). Returns the root node.</summary>
        public LazyNodeExecutor AddNext(LazyNodeExecutor node)
        {
            _next.Add(node ?? throw new ArgumentNullException(nameof(node)));
            return this;
        }

        /// <summary>Add a sibling node (horizontal, sequential). Returns the root node.</summary>
        public LazyNodeExecutor AddNext(Action action) => AddNext(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a sibling node (horizontal, sequential). Returns the root node.</summary>
        public LazyNodeExecutor AddNext<TInput>(Action<TInput> action) => AddNext(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a sibling node (horizontal, sequential). Returns the root node.</summary>
        public LazyNodeExecutor AddNext<TResult>(Func<TResult> func) => AddNext(new LazyNodeExecutor().SetAction(func));

        /// <summary>Add a sibling node (horizontal, sequential). Returns the root node.</summary>
        public LazyNodeExecutor AddNext<TInput, TResult>(Func<TInput, TResult> func) => AddNext(new LazyNodeExecutor().SetAction(func));

        // ---- Tree construction (AddChild) ----

        /// <summary>Add a child node (vertical). Returns the root node.</summary>
        public LazyNodeExecutor AddChild(LazyNodeExecutor node)
        {
            _children.Add(node ?? throw new ArgumentNullException(nameof(node)));
            return this;
        }

        /// <summary>Add a child node (vertical). Returns the root node.</summary>
        public LazyNodeExecutor AddChild(Action action) => AddChild(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a child node (vertical). Returns the root node.</summary>
        public LazyNodeExecutor AddChild<TInput>(Action<TInput> action) => AddChild(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a child node (vertical). Returns the root node.</summary>
        public LazyNodeExecutor AddChild<TResult>(Func<TResult> func) => AddChild(new LazyNodeExecutor().SetAction(func));

        /// <summary>Add a child node (vertical). Returns the root node.</summary>
        public LazyNodeExecutor AddChild<TInput, TResult>(Func<TInput, TResult> func) => AddChild(new LazyNodeExecutor().SetAction(func));

        // ---- Tree construction (AddDependency) ----

        /// <summary>Add a dependency node (horizontal, before). Returns the root node.</summary>
        public LazyNodeExecutor AddDependency(LazyNodeExecutor node)
        {
            _dependencies.Add(node ?? throw new ArgumentNullException(nameof(node)));
            return this;
        }

        /// <summary>Add a dependency node (horizontal, before). Returns the root node.</summary>
        public LazyNodeExecutor AddDependency(Action action) => AddDependency(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a dependency node (horizontal, before). Returns the root node.</summary>
        public LazyNodeExecutor AddDependency<TInput>(Action<TInput> action) => AddDependency(new LazyNodeExecutor().SetAction(action));

        /// <summary>Add a dependency node (horizontal, before). Returns the root node.</summary>
        public LazyNodeExecutor AddDependency<TResult>(Func<TResult> func) => AddDependency(new LazyNodeExecutor().SetAction(func));

        /// <summary>Add a dependency node (horizontal, before). Returns the root node.</summary>
        public LazyNodeExecutor AddDependency<TInput, TResult>(Func<TInput, TResult> func) => AddDependency(new LazyNodeExecutor().SetAction(func));

        // ---- Fluent shortcuts (First) ----

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor First(LazyNodeExecutor node)
        {
            AddDependency(node ?? throw new ArgumentNullException(nameof(node)));
            return node;
        }

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor First(Action action) => First(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor First<TInput>(Action<TInput> action) => First(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor First<TResult>(Func<TResult> func) => First(new LazyNodeExecutor().SetAction(func));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor First<TInput, TResult>(Func<TInput, TResult> func) => First(new LazyNodeExecutor().SetAction(func));

        // ---- Fluent shortcuts (Then) ----

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor Then(LazyNodeExecutor node)
        {
            AddNext(node ?? throw new ArgumentNullException(nameof(node)));
            return node;
        }

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor Then(Action action) => Then(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor Then<TInput>(Action<TInput> action) => Then(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor Then<TResult>(Func<TResult> func) => Then(new LazyNodeExecutor().SetAction(func));

        /// <summary>Adds a sibling node (horizontal, sequential). Returns new node.</summary>
        public LazyNodeExecutor Then<TInput, TResult>(Func<TInput, TResult> func) => Then(new LazyNodeExecutor().SetAction(func));

        // ---- Fluent shortcuts (Nest) ----

        /// <summary>Adds a child node (vertical). Returns new node.</summary>
        public LazyNodeExecutor Nest(LazyNodeExecutor node)
        {
            AddChild(node ?? throw new ArgumentNullException(nameof(node)));
            return node;
        }

        /// <summary>Adds a child node (vertical). Returns new node.</summary>
        public LazyNodeExecutor Nest(Action action) => Nest(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a child node (vertical). Returns new node.</summary>
        public LazyNodeExecutor Nest<TInput>(Action<TInput> action) => Nest(new LazyNodeExecutor().SetAction(action));

        /// <summary>Adds a child node (vertical). Returns new node.</summary>
        public LazyNodeExecutor Nest<TResult>(Func<TResult> func) => Nest(new LazyNodeExecutor().SetAction(func));

        /// <summary>Adds a child node (vertical). Returns new node.</summary>
        public LazyNodeExecutor Nest<TInput, TResult>(Func<TInput, TResult> func) => Nest(new LazyNodeExecutor().SetAction(func));
    }
}
