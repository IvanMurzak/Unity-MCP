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
    public class CalculatorTests
    {
        [Test]
        public void Add_TwoPositiveNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(5f, Calculator.Add(2f, 3f));
        }

        [Test]
        public void Add_TwoNegativeNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(-5f, Calculator.Add(-2f, -3f));
        }

        [Test]
        public void Add_PositiveAndNegativeNumber_ReturnsCorrectSum()
        {
            Assert.AreEqual(1f, Calculator.Add(3f, -2f));
        }

        [Test]
        public void Add_ZeroAndZero_ReturnsZero()
        {
            Assert.AreEqual(0f, Calculator.Add(0f, 0f));
        }

        [Test]
        public void Add_NumberAndZero_ReturnsSameNumber()
        {
            Assert.AreEqual(7f, Calculator.Add(7f, 0f));
        }

        [Test]
        public void Add_FloatingPointNumbers_ReturnsCorrectSum()
        {
            Assert.AreEqual(0.3f, Calculator.Add(0.1f, 0.2f), delta: 0.0001f);
        }
    }
}
