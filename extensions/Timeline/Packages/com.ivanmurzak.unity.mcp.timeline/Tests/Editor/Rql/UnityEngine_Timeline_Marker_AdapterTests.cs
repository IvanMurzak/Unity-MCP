/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    public class UnityEngine_Timeline_Marker_AdapterTests : TimelineTestBase
    {
        private SignalTrack Signals = null!;
        private SignalEmitter Emitter = null!;

        [SetUp]
        public void CreateMarker()
        {
            Signals = AddTrack<SignalTrack>("Signals");
            Emitter = Signals.CreateMarker<SignalEmitter>(2.0);
        }

        [Test]
        public void TimeAndSignalAreWritableAndUndoable()
        {
            var boom = SignalAssetAt("Boom");
            Ok(A + $" | get tracks | get markers | set time = 4, asset = unity.asset(\"{Folder}/Boom.signal\"), emitOnce = true");
            Assert.AreEqual(4.0, Emitter.time, 1e-9);
            Assert.AreSame(boom, Emitter.asset);
            Assert.IsTrue(Emitter.emitOnce);
            Assert.IsTrue(EditorUtility.IsDirty(Emitter));
            Undo.PerformUndo();
            Assert.AreEqual(2.0, Emitter.time, 1e-9);
            Assert.IsNull(Emitter.asset);
        }

        [Test]
        public void NegativeTimeFails()
        {
            Fails(A + " | get tracks | get markers | set time = -1", "must be >= 0");
            Assert.AreEqual(2.0, Emitter.time, 1e-9);
        }

        [Test]
        public void ParentIsTheTrack()
        {
            Assert.AreEqual("Signals", Cell(Read(A + " | get tracks | get markers | select parent.name")));
        }

        [Test]
        public void DeleteRemovesTheMarkerAndIsUndoable()
        {
            Ok(A + " | get tracks | get markers | where time > 1 | call Delete()");
            Assert.AreEqual(0, Signals.GetMarkerCount());
            Undo.PerformUndo();
            Assert.AreEqual(1, Signals.GetMarkerCount());
            Assert.AreEqual(2.0, Signals.GetMarkers().Single().time, 1e-9);
        }
    }
}
