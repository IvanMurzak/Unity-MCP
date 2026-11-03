/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using Rql;
using Rql.Execution;
using UnityEngine;
using static com.IvanMurzak.Unity.MCP.Editor.Rql.RqlValueGuard;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D
{
    /// <summary>Adds the <c>physics</c> page to the <c>settings</c> source (only where the Physics module compiles).</summary>
    public sealed class RqlProjectSettings_Physics_Adapter : RqlTypeAdapter<RqlProjectSettings>
    {
        protected override void Members(MemberTable<RqlProjectSettings> m) =>
            m.Add("physics", s => PhysicsSettings.Instance, description: "Project Settings > Physics");
    }

    /// <summary>Project Settings > Physics (the DynamicsManager asset), <c>settings | get physics</c>.</summary>
    [RqlDescription("Project Settings > Physics: gravity, contact offset, solver iterations, thresholds, query flags (writable, undoable)")]
    public sealed class PhysicsSettings : IRqlUndoOwners
    {
        internal static readonly PhysicsSettings Instance = new PhysicsSettings();

        private PhysicsSettings() { }

        [RqlDescription("Gravity, m/s² (default (0, -9.81, 0)), e.g. set physics.gravity = \"(0, -12, 0)\"")]
        public Vector3 gravity { get => Physics.gravity; set => Physics.gravity = value; }

        [RqlDescription("Default Contact Offset: colliders closer than this generate contacts, meters (> 0)")]
        public float defaultContactOffset
        {
            get => Physics.defaultContactOffset;
            set { Require(value, value > 0f, "> 0"); Physics.defaultContactOffset = value; }
        }

        [RqlDescription("Bounce Threshold: slower contacts do not bounce, m/s (>= 0)")]
        public float bounceThreshold
        {
            get => Physics.bounceThreshold;
            set { Require(value, value >= 0f, ">= 0"); Physics.bounceThreshold = value; }
        }

        [RqlDescription("Sleep Threshold: energy below which a Rigidbody sleeps (>= 0)")]
        public float sleepThreshold
        {
            get => Physics.sleepThreshold;
            set { Require(value, value >= 0f, ">= 0"); Physics.sleepThreshold = value; }
        }

        [RqlDescription("Default Solver Iterations (1 - 255)")]
        public int defaultSolverIterations
        {
            get => Physics.defaultSolverIterations;
            set { Require(value, value >= 1 && value <= 255, "between 1 and 255"); Physics.defaultSolverIterations = value; }
        }

        [RqlDescription("Default Solver Velocity Iterations (1 - 255)")]
        public int defaultSolverVelocityIterations
        {
            get => Physics.defaultSolverVelocityIterations;
            set { Require(value, value >= 1 && value <= 255, "between 1 and 255"); Physics.defaultSolverVelocityIterations = value; }
        }

        [RqlDescription("Default Max Angular Speed of Rigidbodies, rad/s (>= 0)")]
        public float defaultMaxAngularSpeed
        {
            get => Physics.defaultMaxAngularSpeed;
            set { Require(value, value >= 0f, ">= 0"); Physics.defaultMaxAngularSpeed = value; }
        }

        [RqlDescription("Queries Hit Triggers: raycasts and overlaps report trigger colliders")]
        public bool queriesHitTriggers { get => Physics.queriesHitTriggers; set => Physics.queriesHitTriggers = value; }

        [RqlDescription("Queries Hit Backfaces: raycasts hit the back faces of mesh colliders")]
        public bool queriesHitBackfaces { get => Physics.queriesHitBackfaces; set => Physics.queriesHitBackfaces = value; }

        [RqlDescription("Auto Sync Transforms: Transform changes reach physics before every query")]
#pragma warning disable CS0618 // deprecated in Unity 6.3 with no replacement setting; the project setting still applies
        public bool autoSyncTransforms { get => Physics.autoSyncTransforms; set => Physics.autoSyncTransforms = value; }
#pragma warning restore CS0618

        [RqlDescription("Simulation Mode: FixedUpdate, Update or Script")]
        public SimulationMode simulationMode { get => Physics.simulationMode; set => Physics.simulationMode = value; }

        public override string ToString() => $"physics (gravity {gravity})";

        IReadOnlyList<Object> IRqlUndoOwners.UndoOwners => RqlProjectSettings.Asset("DynamicsManager");
    }
}
