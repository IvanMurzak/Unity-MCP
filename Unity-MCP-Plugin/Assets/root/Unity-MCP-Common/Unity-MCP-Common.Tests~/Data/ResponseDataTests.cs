/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using Xunit;
using FluentAssertions;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Data
{
    public class ResponseDataTests
    {
        [Fact]
        public void ResponseData_DefaultConstructor_InitializesWithEmptyValues()
        {
            // Act
            var response = new ResponseData();

            // Assert
            response.RequestID.Should().BeEmpty();
            response.Status.Should().Be(ResponseStatus.Error);
            response.Message.Should().BeNull();
        }

        [Fact]
        public void ResponseData_ParameterizedConstructor_SetsValues()
        {
            // Arrange
            var requestId = "test-request-123";
            var status = ResponseStatus.Success;

            // Act
            var response = new ResponseData(requestId, status);

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(status);
        }

        [Fact]
        public void ResponseData_Constructor_ThrowsOnNullRequestId()
        {
            // Act
            Action act = () => new ResponseData(null!, ResponseStatus.Success);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("requestId");
        }

        [Fact]
        public void ResponseData_Success_CreatesSuccessResponse()
        {
            // Arrange
            var requestId = "success-request";
            var message = "Operation completed successfully";

            // Act
            var response = ResponseData.Success(requestId, message);

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(ResponseStatus.Success);
            response.Message.Should().Be(message);
        }

        [Fact]
        public void ResponseData_Error_CreatesErrorResponse()
        {
            // Arrange
            var requestId = "error-request";
            var message = "Operation failed";

            // Act
            var response = ResponseData.Error(requestId, message);

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(ResponseStatus.Error);
            response.Message.Should().Be(message);
        }

        [Fact]
        public void ResponseData_Processing_CreatesProcessingResponse()
        {
            // Arrange
            var requestId = "processing-request";
            var message = "Operation in progress";

            // Act
            var response = ResponseData.Processing(requestId, message);

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(ResponseStatus.Processing);
            response.Message.Should().Be(message);
        }

        [Fact]
        public void ResponseData_SetRequestID_UpdatesRequestId()
        {
            // Arrange
            var response = new ResponseData();
            var newRequestId = "new-request-id";

            // Act
            var result = response.SetRequestID(newRequestId);

            // Assert
            result.RequestID.Should().Be(newRequestId);
            result.Should().BeSameAs(response); // Fluent API should return same instance
        }
    }

    public class ResponseDataGenericTests
    {
        [Fact]
        public void ResponseDataGeneric_DefaultConstructor_InitializesWithEmptyValues()
        {
            // Act
            var response = new ResponseData<string>();

            // Assert
            response.RequestID.Should().BeEmpty();
            response.Status.Should().Be(ResponseStatus.Error);
            response.Message.Should().BeNull();
            response.Value.Should().BeNull();
        }

        [Fact]
        public void ResponseDataGeneric_CanStoreValue()
        {
            // Arrange
            var response = new ResponseData<int>();
            var value = 42;

            // Act
            response.Value = value;

            // Assert
            response.Value.Should().Be(value);
        }

        [Fact]
        public void ResponseDataGeneric_Success_CreatesSuccessResponseWithValue()
        {
            // Arrange
            var requestId = "test-request";
            var message = "Success";
            var value = "test-value";

            // Act
            var response = ResponseData<string>.Success(requestId, message);
            response.Value = value;

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(ResponseStatus.Success);
            response.Message.Should().Be(message);
            response.Value.Should().Be(value);
        }

        [Fact]
        public void ResponseDataGeneric_Error_CreatesErrorResponse()
        {
            // Arrange
            var requestId = "error-request";
            var message = "Error occurred";

            // Act
            var response = ResponseData<string>.Error(requestId, message);

            // Assert
            response.RequestID.Should().Be(requestId);
            response.Status.Should().Be(ResponseStatus.Error);
            response.Message.Should().Be(message);
            response.Value.Should().BeNull();
        }

        [Fact]
        public void ResponseDataGeneric_SetRequestID_ReturnsTypedInstance()
        {
            // Arrange
            var response = new ResponseData<string>();
            var newRequestId = "new-request-id";

            // Act
            var result = response.SetRequestID(newRequestId);

            // Assert
            result.Should().BeOfType<ResponseData<string>>();
            result.RequestID.Should().Be(newRequestId);
        }

        [Fact]
        public void ResponseDataGeneric_WithComplexType_StoresComplexValue()
        {
            // Arrange
            var complexValue = new { Name = "Test", Value = 123 };
            var response = ResponseData<object>.Success("request-id");

            // Act
            response.Value = complexValue;

            // Assert
            response.Value.Should().BeSameAs(complexValue);
        }
    }
}
