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
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.AtomicApi
{
    /// <summary>
    /// Small POCO test fixtures used by the atomic-API EditMode tests
    /// (TryModifyAt / TryPatch / TryReadAt / View / Grep). The shape mirrors
    /// the SolarSystem fixture used by ReflectorNet.Tests but is defined here
    /// so it does not depend on ReflectorNet's own test assembly, which is not
    /// shipped to Unity. Naming and field types match
    /// <c>com.IvanMurzak.ReflectorNet.Tests.Model.SolarSystem.CelestialBody</c>
    /// closely so reviewers can correlate the assertions across the two repos.
    /// </summary>
    public class CelestialBody
    {
        [JsonInclude]
        public float orbitRadius = 10f;

        [JsonInclude]
        public float orbitSpeed = 1f;

        [JsonInclude]
        public float rotationSpeed = 1f;

        [JsonInclude]
        public string name = string.Empty;
    }

    /// <summary>
    /// A small POCO graph that mirrors the Unity-side shape of a celestial-body
    /// container used in real Unity-MCP tools (root field + array + dictionary).
    /// </summary>
    public class StarSystemPoco
    {
        [JsonInclude]
        public CelestialBody[]? celestialBodies;

        [JsonInclude]
        public Dictionary<string, int> config = new Dictionary<string, int>();

        [JsonInclude]
        public Dictionary<int, string> lookup = new Dictionary<int, string>();

        [JsonInclude]
        public float globalOrbitSpeedMultiplier = 1f;

        [JsonInclude]
        public float globalSizeMultiplier = 1f;
    }

    /// <summary>Polymorphism fixture for the TryPatch "$type" hint scenario.</summary>
    public class Animal
    {
        [JsonInclude]
        public string name = string.Empty;
    }

    /// <summary>Polymorphism fixture — subtype of Animal used by the "$type" hint test.</summary>
    public class Dog : Animal
    {
        [JsonInclude]
        public string breed = string.Empty;
    }

    /// <summary>Container that holds an Animal — used for polymorphism / type-replacement tests.</summary>
    public class AnimalContainer
    {
        [JsonInclude]
        public Animal? animal;
    }

    /// <summary>
    /// Graph with a self-reference, used to confirm View / Grep stop traversing
    /// when they hit a cycle and do not recurse infinitely.
    /// </summary>
    public class CyclicNode
    {
        [JsonInclude]
        public string label = string.Empty;

        [JsonInclude]
        public float weight = 0f;

        [JsonInclude]
        public CyclicNode? next;
    }
}
