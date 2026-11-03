/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    public class UnityEngine_AI_NavMeshAgent_AdapterTests : NavigationTestBase
    {
        [Test]
        public void AddAndConfigure()
        {
            new GameObject("Enemy");
            Ok(M("objects | where name == \"Enemy\" | call AddComponent(\"NavMeshAgent\")"));
            Ok(M(Agents + " | set speed = 5.5, acceleration = 12, angularSpeed = 240, stoppingDistance = 1.25, agentType = \"Humanoid\""));
            var a = Object.FindAnyObjectByType<NavMeshAgent>();
            Assert.AreEqual(5.5f, a.speed, 1e-6f);
            Assert.AreEqual(12f, a.acceleration, 1e-6f);
            Assert.AreEqual(240f, a.angularSpeed, 1e-6f);
            Assert.AreEqual(1.25f, a.stoppingDistance, 1e-6f);
            Assert.AreEqual(0, a.agentTypeID);
        }

        [Test]
        public void AreaMaskAsNames()
        {
            var a = new GameObject("Enemy").AddComponent<NavMeshAgent>();
            Assert.AreEqual("[\"All\"]", Cell(Q(Agents + " | select areaNames")).Replace(" ", ""));
            Ok(M(Agents + " | set areaNames = [\"Walkable\", \"Jump\"]"));
            Assert.AreEqual((1 << 0) | (1 << 2), a.areaMask);
            Assert.AreEqual("[\"Walkable\",\"Jump\"]", Cell(Q(Agents + " | select areaNames")).Replace(" ", ""));
            Ok(M(Agents + " | set areaNames = [\"All\"]"));
            Assert.AreEqual(NavMesh.AllAreas, a.areaMask);
            Assert.IsFalse(M(Agents + " | set areaNames = [\"Lava\"]").Ok);
            Assert.AreEqual(NavMesh.AllAreas, a.areaMask);
        }

        [Test]
        public void OffTheNavMeshEverythingReadsWithoutErrors()
        {
            new GameObject("Enemy").AddComponent<NavMeshAgent>();
            // Reading every member of an agent that is not on a NavMesh must not log Unity errors (isStopped, remainingDistance …).
            var r = Q(Agents);   // items are encoded with their members
            Ok(r);
            var describe = RqlUnitySchema.Render(RqlUnity.Engine, "NavMeshAgent");
            StringAssert.Contains("SetDestination(", describe);
            Ok(Q(Agents + " | select isStopped remainingDistance destination isOnNavMesh hasPath"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OperationsOffTheNavMeshReturnFalseWithoutErrors()
        {
            new GameObject("Enemy").AddComponent<NavMeshAgent>();
            var r = M(Agents + " | call SetDestination(\"(10, 0, 5)\")");
            Ok(r);
            StringAssert.Contains("false", Table(r));
            Ok(M(Agents + " | call ResetPath()"));
            Assert.IsFalse(M(Agents + " | set isStopped = true").Ok, "isStopped needs a NavMesh");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WarpInEditModeIsANoOp()
        {
            // Agents are simulated only in Play Mode: even above a baked NavMesh, Warp reports false and logs no error.
            CreateGround();
            Ok(M(Surfaces + " | call Bake()"));
            new GameObject("Enemy").AddComponent<NavMeshAgent>();
            var r = M(Agents + " | call Warp(\"(2, 0, 3)\")");
            Ok(r);
            StringAssert.Contains("Warp(Vector3 point)|false", Table(r));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
