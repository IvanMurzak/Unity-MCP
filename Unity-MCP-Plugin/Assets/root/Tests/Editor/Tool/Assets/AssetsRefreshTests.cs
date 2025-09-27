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
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using System;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class AssetsRefreshTests : BaseTest
    {
        [Test]
        public void Refresh_WithValidRequestId_ReturnsSuccess()
        {
            // Arrange
            var requestId = Guid.NewGuid().ToString();

            // Act
            var result = Tool_Assets.Refresh(requestId);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(requestId, result.RequestID, "RequestID should match the provided value");
            
            // The result should be either Success or Processing depending on whether compilation starts
            Assert.IsTrue(
                result.Status == ResponseStatus.Success || result.Status == ResponseStatus.Processing,
                $"Status should be Success or Processing, but was: {result.Status}"
            );
            
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains("AssetDatabase refreshed"), "Message should indicate AssetDatabase was refreshed");
        }

        [Test]
        public void Refresh_WithNullRequestId_ReturnsError()
        {
            // Act
            var result = Tool_Assets.Refresh(null);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Status should be Error");
            
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains("Original request with valid RequestID must be provided"), 
                "Message should indicate RequestID is required");
        }

        [Test]
        public void Refresh_WithEmptyRequestId_ReturnsError()
        {
            // Act
            var result = Tool_Assets.Refresh("");

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Status should be Error");
            
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains("Original request with valid RequestID must be provided"), 
                "Message should indicate RequestID is required");
        }

        [Test]
        public void Refresh_WithWhitespaceRequestId_ReturnsError()
        {
            // Act
            var result = Tool_Assets.Refresh("   ");

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Status should be Error");
            
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains("Original request with valid RequestID must be provided"), 
                "Message should indicate RequestID is required");
        }
    }
}