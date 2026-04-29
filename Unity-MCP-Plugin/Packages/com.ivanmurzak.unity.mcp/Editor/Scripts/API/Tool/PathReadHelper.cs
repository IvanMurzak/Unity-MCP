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

        /// <summary>
        /// Reads each path via <see cref="Reflector.TryReadAt"/> and aggregates the results into a single
        /// <see cref="SerializedMember"/> envelope: top-level <c>fields[i].name</c> is the requested path,
        /// <c>fields[i]</c> contents are the serialised value at that path. When a path fails to navigate,
        /// the entry is replaced with a sentinel field whose name is the path and value is null; per-path
        /// diagnostics are accumulated into <paramref name="aggregateLogs"/> for caller logging.
        /// </summary>
        public static SerializedMember BuildPathReadAggregate(
            Reflector reflector,
            object obj,
            string? rootName,
            IReadOnlyList<string> paths,
            ILogger? logger)
        {
            var fields = new SerializedMemberList(paths.Count);
            foreach (var path in paths)
            {
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
                        typeName = "<unresolved>"
                    });

                    foreach (var entry in perPathLogs)
                        logger?.LogWarning($"[path-read] '{path}': {entry}");
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
