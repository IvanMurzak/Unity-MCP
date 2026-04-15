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
#if UNITY_6000_5_OR_NEWER
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>
    /// Helpers for reconstructing <see cref="UnityEngine.EntityId"/> values from
    /// numeric representations that may appear in JSON, strings, or test fixtures.
    /// <para>
    /// Unity 6.5+ encodes an EntityId's raw data as
    /// <c>(0x7E2510500000000UL | (uint)legacyInstanceId)</c>.
    /// <see cref="EntityId.ToString"/> emits only the low 32 bits as a signed int
    /// (the legacy instance ID display), while <see cref="EntityId.ToULong"/>
    /// emits the full 64-bit raw value. Both forms show up in JSON payloads —
    /// this utility accepts either and rebuilds the correct EntityId so that
    /// <c>EditorUtility.EntityIdToObject</c> can resolve the original object.
    /// </para>
    /// </summary>
    public static class EntityIdUtils
    {
        /// <summary>
        /// Magic prefix applied to the low 32 bits to form a valid EntityId raw value.
        /// Matches Unity's internal <c>EntityId.Parse</c> / implicit int conversion.
        /// </summary>
        public const ulong EntityIdMagic = 0x7E2510500000000UL;

        /// <summary>
        /// Builds an <see cref="EntityId"/> from a legacy int instance ID
        /// (the form produced by <see cref="EntityId.ToString"/>).
        /// </summary>
        public static EntityId FromLegacyInstanceId(int instanceId)
            => EntityId.FromULong(EntityIdMagic | (uint)instanceId);

        /// <summary>
        /// Builds an <see cref="EntityId"/> from a numeric value that may be either:
        /// the legacy int form (<see cref="EntityId.ToString"/>) when it fits in
        /// <see cref="int"/> range, or the raw ulong form
        /// (<see cref="EntityId.ToULong"/>) otherwise.
        /// </summary>
        public static EntityId FromNumber(long value)
            => value >= int.MinValue && value <= int.MaxValue
                ? FromLegacyInstanceId((int)value)
                : EntityId.FromULong(unchecked((ulong)value));

        /// <summary>
        /// Builds an <see cref="EntityId"/> from a raw ulong
        /// (<see cref="EntityId.ToULong"/>).
        /// </summary>
        public static EntityId FromRawValue(ulong value) => EntityId.FromULong(value);
    }
}
#endif
