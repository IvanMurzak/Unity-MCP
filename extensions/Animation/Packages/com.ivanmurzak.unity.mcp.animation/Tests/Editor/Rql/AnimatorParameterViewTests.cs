/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class AnimatorParameterViewTests : AnimationTestBase
    {
        [Test]
        public void ParametersAreReadable()
        {
            Controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            var r = Table(Ok(Q(P + " | get parameters | select index name type defaultFloat defaultBool")));
            StringAssert.Contains("Speed", r);
            StringAssert.Contains("Float", r);
            StringAssert.Contains("Grounded", r);
            Assert.AreEqual(1L, Count(P + " | get parameters | where type == \"Bool\" | count"));
        }

        [Test]
        public void DefaultsAreWritable()
        {
            Controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            Controller.AddParameter("Lives", AnimatorControllerParameterType.Int);
            Ok(P + " | set parameters[0].defaultFloat = 1.5, parameters[1].defaultBool = true, parameters[2].defaultInt = 3");
            var ps = Controller.parameters;
            Assert.AreEqual(1.5f, ps[0].defaultFloat, 1e-5f);
            Assert.IsTrue(ps[1].defaultBool);
            Assert.AreEqual(3, ps[2].defaultInt);
        }

        [Test]
        public void WrongTypedDefaultFails()
        {
            Fails(P + " | set parameters[0].defaultBool = true", "is a Float");
            Assert.IsFalse(Controller.parameters[0].defaultBool);
        }

        [Test]
        public void NameAndTypeAreReadOnly()
        {
            var r = M(P + " | set parameters[0].name = \"Velocity\"");
            Assert.IsFalse(r.Ok, Table(r));
            Assert.AreEqual("Speed", Controller.parameters[0].name);
        }

        [Test]
        public void ParameterWritesAreUndoable()
        {
            Undo.ClearAll();
            Ok(P + " | get parameters | where name == \"Speed\" | set defaultFloat = 2");
            Undo.PerformUndo();
            Assert.AreEqual(0f, Controller.parameters[0].defaultFloat, 1e-5f);
        }
    }
}
