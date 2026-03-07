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
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class MathUtilsTests
    {
        [Test]
        public void Add_TwoPositiveIntegers_ReturnsCorrectSum()
        {
            Assert.AreEqual(5, MathUtils.Add(2, 3));
        }

        [Test]
        public void Add_ZeroAndPositive_ReturnsPositive()
        {
            Assert.AreEqual(7, MathUtils.Add(0, 7));
        }

        [Test]
        public void Add_PositiveAndZero_ReturnsPositive()
        {
            Assert.AreEqual(7, MathUtils.Add(7, 0));
        }

        [Test]
        public void Add_TwoZeros_ReturnsZero()
        {
            Assert.AreEqual(0, MathUtils.Add(0, 0));
        }

        [Test]
        public void Add_NegativeNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(-5, MathUtils.Add(-2, -3));
        }

        [Test]
        public void Add_PositiveAndNegative_ReturnsCorrectSum()
        {
            Assert.AreEqual(1, MathUtils.Add(4, -3));
        }

        [Test]
        public void Add_NegativeAndPositive_ReturnsCorrectSum()
        {
            Assert.AreEqual(-1, MathUtils.Add(-4, 3));
        }

        [Test]
        public void Add_LargeNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(2000000000, MathUtils.Add(1000000000, 1000000000));
        }
    }
}
