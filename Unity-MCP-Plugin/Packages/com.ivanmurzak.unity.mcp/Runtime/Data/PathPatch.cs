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

        [Description(
            "The new value to write at the path. " +
            "Use the standard SerializedMember envelope: 'typeName' + 'value' for primitives, " +
            "or nested 'fields'/'props' for complex types. " +
            "Required.")]
        public SerializedMember Value { get; set; } = new SerializedMember();
    }
}
