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
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    /// <summary>
    /// A single path-scoped modification routed through
    /// <see cref="com.IvanMurzak.ReflectorNet.Reflector.TryModifyAt(ref object?, string, SerializedMember, System.Type?, int, com.IvanMurzak.ReflectorNet.Model.Logs?, System.Reflection.BindingFlags, Microsoft.Extensions.Logging.ILogger?)"/>.
    /// Each entry targets one field, array element, or dictionary entry by path.
    /// </summary>
    public class PathPatch
    {
        [Description(
            "Slash-delimited path to the target field/element/entry. " +
            "Plain segment navigates a field or property (e.g. 'admin' or 'admin/name'). " +
            "Use '[i]' for array/list index (e.g. 'planets/[0]/orbitRadius'). " +
            "Use '[key]' for dictionary entry (e.g. 'config/[timeout]'). " +
            "A leading '#/' is stripped automatically. " +
            "Required.")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The new value to write at <see cref="Path"/>.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>new SerializedMember()</c> so JSON deserialisation of a payload that
        /// omits <c>Value</c> still produces a non-null instance — but writing that default is
        /// almost certainly a mistake (it would replace the target with an empty
        /// <see cref="SerializedMember"/>). Always populate <see cref="Value"/> explicitly with
        /// the standard envelope (<c>typeName</c> + <c>value</c> for primitives, or nested
        /// <c>fields</c>/<c>props</c> for complex types) before passing the patch to a tool.
        /// </remarks>
        [Description(
            "The new value to write at the path. " +
            "Use the standard SerializedMember envelope: 'typeName' + 'value' for primitives, " +
            "or nested 'fields'/'props' for complex types. " +
            "Required — omitting it overwrites the target with a default empty SerializedMember.")]
        public SerializedMember Value { get; set; } = new SerializedMember();
    }
}
