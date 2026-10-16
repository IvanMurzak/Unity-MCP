/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.InputSystem
{
    public class BindingViewTests : InputSystemTestBase
    {
        private static string Bindings(string action) => Action(action) + " | get bindings";

        [Test]
        public void BindingsAreReadableInOrder()
        {
            var r = Read(Bindings("Move") + " | select action index path name groups isComposite isPartOfComposite");
            Assert.AreEqual(5, r.Rows!.Count, Table(r));
            Assert.AreEqual("Player/Move", Cell(r));
            Assert.AreEqual("2DVector", Cell(r, 2));
            Assert.AreEqual("true", Cell(r, 5));
            Assert.AreEqual("up", Cell(r, 3, 1));
            Assert.AreEqual("Keyboard&Mouse", Cell(r, 4, 1));
            Assert.AreEqual("true", Cell(r, 6, 1));
            Assert.AreEqual("4", Cell(r, 1, 4));
        }

        [Test]
        public void WritesReplaceTheValueAndPersist()
        {
            Ok(Bindings("Jump") + " | where path == \"<Keyboard>/space\" | set path = \"<Keyboard>/enter\", interactions = \"press\", processors = \"invert\"");
            // A second write replaces (the Input System's With* syntax would append).
            Ok(Bindings("Jump") + " | where path == \"<Keyboard>/enter\" | set interactions = \"tap\", groups = \"Keyboard&Mouse;Gamepad\"");
            using var saved = FromFile();
            var b = saved.Asset.FindAction("Player/Jump", true).bindings[0];
            Assert.AreEqual("<Keyboard>/enter", b.path);
            Assert.AreEqual("tap", b.interactions);
            Assert.AreEqual("invert", b.processors);
            Assert.AreEqual("Keyboard&Mouse;Gamepad", b.groups);
        }

        [Test]
        public void ClearingGroupsPersists()
        {
            Ok(Bindings("Jump") + " | where path == \"<Gamepad>/buttonSouth\" | set groups = \"\"");
            using var saved = FromFile();
            Assert.IsTrue(string.IsNullOrEmpty(saved.Asset.FindAction("Player/Jump", true).bindings[1].groups));
        }

        [Test]
        public void EveryMatchingBindingIsWritten()
        {
            var r = Ok(Bindings("Move") + " | where isPartOfComposite | set groups = \"Gamepad\"");
            Assert.AreEqual(4, r.Rows!.Count, Table(r));
            using var saved = FromFile();
            Assert.IsTrue(saved.Asset.FindAction("Player/Move", true).bindings.Skip(1).All(b => b.groups == "Gamepad"));
        }

        [Test]
        public void CompositePartsAndRootsAreValidated()
        {
            Ok(Bindings("Move") + " | where name == \"up\" | set name = \"Down\"");
            Assert.AreEqual("down", Asset.FindAction("Player/Move", true).bindings[1].name);
            Fails(Bindings("Move") + " | where name == \"left\" | set name = \"top\"", "has no part 'top'");
            Fails(Bindings("Move") + " | where isComposite | set path = \"<Keyboard>/w\"", "unknown composite");
            Fails(Bindings("Jump") + " | where path == \"<Keyboard>/space\" | set groups = \"Touch\"", "is not a control scheme");
            Fails(Bindings("Jump") + " | where path == \"<Keyboard>/space\" | set interactions = \"hodl\"", "unknown interaction");
        }

        [Test]
        public void RemovingACompositeRemovesItsParts()
        {
            var r = Ok(Bindings("Move") + " | where isComposite | call Remove()");
            Assert.AreEqual("0", Result(r));
            using var saved = FromFile();
            Assert.AreEqual(0, saved.Asset.FindAction("Player/Move", true).bindings.Count);
        }

        [Test]
        public void RemovingSeveralBindingsDoesNotShiftTheOthers()
        {
            // Root first, then its parts (already gone with it), then a binding of another action.
            Ok(A + " | get actions | get bindings | where groups == \"Keyboard&Mouse\" or isComposite | call Remove()");
            using var saved = FromFile();
            CollectionAssert.AreEqual(new[] { "<Gamepad>/buttonSouth" }, Paths(saved.Asset, "Player/Jump"));
            Assert.AreEqual(0, saved.Asset.FindAction("Player/Move", true).bindings.Count);
            Assert.AreEqual(0, saved.Asset.FindAction("UI/Click", true).bindings.Count);
        }

        [Test]
        public void AViewFollowsItsBindingAfterOthersAreRemoved()
        {
            Ok(Bindings("Jump") + " | where path == \"<Keyboard>/space\" | call Remove()");
            var r = Read(Bindings("Jump") + " | select index path");
            Assert.AreEqual("0", Cell(r));
            Assert.AreEqual("<Gamepad>/buttonSouth", Cell(r, 1));
        }
    }
}
