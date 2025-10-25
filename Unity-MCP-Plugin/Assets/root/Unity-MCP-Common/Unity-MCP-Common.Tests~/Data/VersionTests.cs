/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using Xunit;
using FluentAssertions;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Data
{
    public class VersionTests
    {
        [Fact]
        public void Version_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var version = new Version();

            // Assert
            version.Api.Should().Be("1.0.0");
            version.Plugin.Should().Be("1.0.0");
            version.UnityVersion.Should().BeEmpty();
        }

        [Fact]
        public void Version_CanSetApiVersion()
        {
            // Arrange
            var version = new Version();

            // Act
            version.Api = "2.0.0";

            // Assert
            version.Api.Should().Be("2.0.0");
        }

        [Fact]
        public void Version_CanSetPluginVersion()
        {
            // Arrange
            var version = new Version();

            // Act
            version.Plugin = "3.5.2";

            // Assert
            version.Plugin.Should().Be("3.5.2");
        }

        [Fact]
        public void Version_CanSetUnityVersion()
        {
            // Arrange
            var version = new Version();

            // Act
            version.UnityVersion = "2022.3.10f1";

            // Assert
            version.UnityVersion.Should().Be("2022.3.10f1");
        }

        [Fact]
        public void Version_ObjectInitializer_ShouldSetAllProperties()
        {
            // Act
            var version = new Version
            {
                Api = "2.1.0",
                Plugin = "4.3.2",
                UnityVersion = "2023.1.5f1"
            };

            // Assert
            version.Api.Should().Be("2.1.0");
            version.Plugin.Should().Be("4.3.2");
            version.UnityVersion.Should().Be("2023.1.5f1");
        }
    }
}
