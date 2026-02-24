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
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CommandLineArgs"/> and <see cref="ConnectResult"/>.
    ///
    /// Note: Testing <see cref="CommandLineArgs.EnforceConnect"/> with a valid URL
    /// requires the full Unity MCP plugin stack (UnityMcpPlugin singleton, connection
    /// manager, etc.) and is therefore covered only at integration-test level.
    /// Unit tests here focus on error-path behaviour and the <see cref="ConnectResult"/>
    /// value type, which have no external dependencies.
    /// </summary>
    [TestFixture]
    public class CommandLineArgsTests
    {
        // -----------------------------------------------------------------------
        // ConnectResult — factory methods
        // -----------------------------------------------------------------------

        [Test]
        public void ConnectResult_Ok_HasSuccessTrue()
        {
            var result = ConnectResult.Ok();

            Assert.IsTrue(result.Success, "Ok() result should have Success = true");
        }

        [Test]
        public void ConnectResult_Ok_HasNullErrorFields()
        {
            var result = ConnectResult.Ok();

            Assert.IsNull(result.ErrorMessage, "Ok() result should have null ErrorMessage");
            Assert.IsNull(result.ErrorType,    "Ok() result should have null ErrorType");
            Assert.IsNull(result.StackTrace,   "Ok() result should have null StackTrace");
        }

        [Test]
        public void ConnectResult_Fail_WithMessage_HasSuccessFalse()
        {
            var result = ConnectResult.Fail("something went wrong");

            Assert.IsFalse(result.Success, "Fail(message) result should have Success = false");
        }

        [Test]
        public void ConnectResult_Fail_WithMessage_PreservesMessage()
        {
            const string expected = "some descriptive error";
            var result = ConnectResult.Fail(expected);

            Assert.AreEqual(expected, result.ErrorMessage,
                "Fail(message) result should preserve the ErrorMessage");
        }

        [Test]
        public void ConnectResult_Fail_WithMessage_HasNullExceptionFields()
        {
            var result = ConnectResult.Fail("plain error");

            Assert.IsNull(result.ErrorType,  "Fail(message) should leave ErrorType null");
            Assert.IsNull(result.StackTrace, "Fail(message) should leave StackTrace null");
        }

        [Test]
        public void ConnectResult_Fail_WithException_HasSuccessFalse()
        {
            var ex = new InvalidOperationException("oops");
            var result = ConnectResult.Fail(ex);

            Assert.IsFalse(result.Success, "Fail(exception) result should have Success = false");
        }

        [Test]
        public void ConnectResult_Fail_WithException_CapturesMessage()
        {
            var ex = new InvalidOperationException("test exception message");
            var result = ConnectResult.Fail(ex);

            Assert.AreEqual("test exception message", result.ErrorMessage,
                "Fail(exception) should capture exception.Message");
        }

        [Test]
        public void ConnectResult_Fail_WithException_CapturesFullTypeName()
        {
            var ex = new InvalidOperationException("msg");
            var result = ConnectResult.Fail(ex);

            Assert.IsTrue(
                result.ErrorType?.Contains("InvalidOperationException"),
                $"ErrorType should contain the exception type name. Actual: '{result.ErrorType}'");
        }

        [Test]
        public void ConnectResult_Fail_WithException_WithStackTrace_CapturesStackTrace()
        {
            // Throw and catch to populate a real stack trace
            InvalidOperationException? ex = null;
            try { throw new InvalidOperationException("trace test"); }
            catch (InvalidOperationException e) { ex = e; }

            var result = ConnectResult.Fail(ex!);

            Assert.IsNotNull(result.StackTrace, "Fail(exception) should capture the stack trace");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.StackTrace),
                "Captured stack trace should not be empty");
        }

        [Test]
        public void ConnectResult_Fail_WithException_WithoutStackTrace_HasNullOrEmptyStackTrace()
        {
            // An exception that was never thrown has no stack trace
            var ex = new ArgumentNullException("param1");
            var result = ConnectResult.Fail(ex);

            // StackTrace may be null or empty when the exception was never thrown
            var hasNoTrace = result.StackTrace == null || result.StackTrace.Length == 0;
            Assert.IsTrue(hasNoTrace,
                "Fail(exception with no stack trace) should have null/empty StackTrace");
        }

        // -----------------------------------------------------------------------
        // ConnectResult — ToString
        // -----------------------------------------------------------------------

        [Test]
        public void ConnectResult_ToString_SuccessCase_ContainsSuccessMarker()
        {
            var result = ConnectResult.Ok();
            var str = result.ToString();

            Assert.IsTrue(str.Contains("[Success]"),
                $"ToString() of a success result should contain '[Success]'. Actual: '{str}'");
        }

        [Test]
        public void ConnectResult_ToString_FailureCase_ContainsErrorMarker()
        {
            var result = ConnectResult.Fail("bad url");
            var str = result.ToString();

            Assert.IsTrue(str.Contains("[Error]"),
                $"ToString() of a failure result should contain '[Error]'. Actual: '{str}'");
        }

        [Test]
        public void ConnectResult_ToString_FailureCase_ContainsErrorMessage()
        {
            var result = ConnectResult.Fail("meaningful error description");
            var str = result.ToString();

            Assert.IsTrue(str.Contains("meaningful error description"),
                $"ToString() should include the ErrorMessage. Actual: '{str}'");
        }

        [Test]
        public void ConnectResult_ToString_FailureWithException_ContainsExceptionType()
        {
            var result = ConnectResult.Fail(new ArgumentException("bad arg"));
            var str = result.ToString();

            Assert.IsTrue(str.Contains("ArgumentException"),
                $"ToString() should include the exception type. Actual: '{str}'");
        }

        // -----------------------------------------------------------------------
        // ConnectResult — equality / value semantics
        // -----------------------------------------------------------------------

        [Test]
        public void ConnectResult_TwoOkResults_AreEqual()
        {
            var a = ConnectResult.Ok();
            var b = ConnectResult.Ok();

            // ConnectResult is a readonly struct, so == uses value equality
            Assert.AreEqual(a, b, "Two Ok() results should be equal");
        }

        [Test]
        public void ConnectResult_OkAndFail_AreNotEqual()
        {
            var ok   = ConnectResult.Ok();
            var fail = ConnectResult.Fail("error");

            Assert.AreNotEqual(ok, fail, "Ok() and Fail() results should not be equal");
        }

        // -----------------------------------------------------------------------
        // CommandLineArgs.EnforceConnect — error-path (no Unity MCP stack needed)
        // -----------------------------------------------------------------------

        [Test]
        public void EnforceConnect_WithEmptyUrl_ReturnsFail()
        {
            var result = CommandLineArgs.EnforceConnect(string.Empty);

            Assert.IsFalse(result.Success,
                "EnforceConnect with an empty URL should return a failure result");
        }

        [Test]
        public void EnforceConnect_WithNullUrl_ReturnsFail()
        {
            var result = CommandLineArgs.EnforceConnect(null!);

            Assert.IsFalse(result.Success,
                "EnforceConnect with null should return a failure result");
        }

        [Test]
        public void EnforceConnect_WithWhitespaceUrl_ReturnsFail()
        {
            var result = CommandLineArgs.EnforceConnect("   ");

            Assert.IsFalse(result.Success,
                "EnforceConnect with whitespace-only URL should return a failure result");
        }

        [Test]
        public void EnforceConnect_WithEmptyUrl_ReturnsNonNullErrorMessage()
        {
            var result = CommandLineArgs.EnforceConnect(string.Empty);

            Assert.IsNotNull(result.ErrorMessage,
                "Failure result from empty URL should carry a non-null ErrorMessage");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage),
                "Failure result ErrorMessage should not be empty");
        }

        // -----------------------------------------------------------------------
        // ConnectResultRelativePath constant
        // -----------------------------------------------------------------------

        [Test]
        public void ConnectResultRelativePath_IsNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(CommandLineArgs.ConnectResultRelativePath),
                "ConnectResultRelativePath should be a non-empty relative path");
        }

        [Test]
        public void ConnectResultRelativePath_StartsWithLibrary()
        {
            Assert.IsTrue(
                CommandLineArgs.ConnectResultRelativePath.StartsWith("Library",
                    StringComparison.OrdinalIgnoreCase),
                "ConnectResultRelativePath should be inside the Library folder");
        }

        [Test]
        public void ConnectResultRelativePath_EndsWithJson()
        {
            Assert.IsTrue(
                CommandLineArgs.ConnectResultRelativePath.EndsWith(".json",
                    StringComparison.OrdinalIgnoreCase),
                "ConnectResultRelativePath should point to a .json file");
        }
    }
}
