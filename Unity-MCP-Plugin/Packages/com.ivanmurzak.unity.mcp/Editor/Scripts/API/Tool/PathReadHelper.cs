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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Shared helpers for path-based read tools. Centralises the construction of an aggregate
    /// <see cref="SerializedMember"/> envelope whose <c>fields</c> mirror one
    /// <see cref="Reflector.TryReadAt"/> call per requested path. The envelope is the same
    /// shape callers already expect from the legacy single-call <c>Serialize</c> path, so
    /// path-scoped reads slot in without breaking the response schema.
    /// </summary>
    internal static class PathReadHelper
    {
        public const string PathReadAggregateTypeName = "Unity-MCP.PathReadAggregate";
        public const string EmptyPathTypeName = "<empty-path>";
        public const string UnresolvedTypeName = "<unresolved>";

        /// <summary>
        /// Reads each path via <see cref="Reflector.TryReadAt"/> and aggregates the results into a single
        /// <see cref="SerializedMember"/> envelope: top-level <c>fields[i].name</c> is the requested path,
        /// <c>fields[i]</c> contents are the serialised value at that path. When a path fails to navigate,
        /// the entry is replaced with a sentinel field whose name is the path and value is null. Per-path
        /// diagnostics are emitted via <paramref name="logger"/> and, when supplied, also appended to
        /// <paramref name="aggregateLogs"/> so callers that maintain a structured log accumulator can
        /// surface them back to the AI agent.
        /// </summary>
        /// <param name="reflector">The reflector that performs the per-path read.</param>
        /// <param name="obj">Object to read paths from.</param>
        /// <param name="rootName">Optional name for the aggregate envelope.</param>
        /// <param name="paths">Paths to read. Null or empty entries are skipped with an
        /// <see cref="EmptyPathTypeName"/> sentinel field rather than silently navigating
        /// the root (which would defeat the token-saving purpose of path-scoped reads).</param>
        /// <param name="logger">Optional logger for per-path diagnostics.</param>
        /// <param name="aggregateLogs">Optional structured log accumulator. When non-null,
        /// per-path failure entries are appended so the caller can surface them in its response.</param>
        public static SerializedMember BuildPathReadAggregate(
            Reflector reflector,
            object obj,
            string? rootName,
            IReadOnlyList<string> paths,
            ILogger? logger,
            Logs? aggregateLogs = null)
        {
            var fields = new SerializedMemberList(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                if (string.IsNullOrEmpty(path))
                {
                    fields.Add(new SerializedMember
                    {
                        name = path,
                        typeName = EmptyPathTypeName
                    });
                    var emptyMsg = $"[path-read] paths[{i}] is empty or null and was skipped.";
                    logger?.LogWarning(emptyMsg);
                    aggregateLogs?.Warning(emptyMsg);
                    continue;
                }

                var perPathLogs = new Logs();
                if (reflector.TryReadAt(obj, path, out var member, logs: perPathLogs, logger: logger) && member != null)
                {
                    member.name = path;
                    fields.Add(member);
                }
                else
                {
                    fields.Add(new SerializedMember
                    {
                        name = path,
                        typeName = UnresolvedTypeName
                    });

                    foreach (var entry in perPathLogs)
                    {
                        var msg = $"[path-read] '{path}': {entry}";
                        logger?.LogWarning(msg);
                        aggregateLogs?.Warning(msg);
                    }
                }
            }

            return new SerializedMember
            {
                name = rootName,
                typeName = PathReadAggregateTypeName,
                fields = fields
            };
        }
    }
}
