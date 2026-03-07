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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Calculator
{
    public class CalculatorTests
    {
        [Test]
        public void Add_PositiveNumbers_ReturnsSum()
        {
            var result = Calculator.Add(2f, 3f);
            Assert.AreEqual(5f, result, "2 + 3 should equal 5");
        }

        [Test]
        public void Add_NegativeNumbers_ReturnsSum()
        {
            var result = Calculator.Add(-4f, -6f);
            Assert.AreEqual(-10f, result, "-4 + -6 should equal -10");
        }

        [Test]
        public void Add_PositiveAndNegative_ReturnsSum()
        {
            var result = Calculator.Add(10f, -3f);
            Assert.AreEqual(7f, result, "10 + -3 should equal 7");
        }

        [Test]
        public void Add_Zeros_ReturnsZero()
        {
            var result = Calculator.Add(0f, 0f);
            Assert.AreEqual(0f, result, "0 + 0 should equal 0");
        }

        [Test]
        public void Add_WithZero_ReturnsSameNumber()
        {
            var result = Calculator.Add(42f, 0f);
            Assert.AreEqual(42f, result, "42 + 0 should equal 42");
        }

        [Test]
        public void Add_FloatingPoint_ReturnsCorrectSum()
        {
            var result = Calculator.Add(1.5f, 2.5f);
            Assert.AreEqual(4f, result, 0.0001f, "1.5 + 2.5 should equal 4");
        }
    }
}
