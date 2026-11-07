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
using com.IvanMurzak.Unity.MCP.Editor.Services;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>
    /// <c>UNITY_MCP_CREDENTIALS_DIR</c> validation: unset means the OS profile's store, an absolute path
    /// relocates it, anything relative is rejected (thrown by <see cref="CredentialsDirectory.Resolve"/>,
    /// logged and ignored by <see cref="CredentialsDirectory.ResolveOrDefault"/>).
    /// </summary>
    public class CredentialsDirectoryTests
    {
        static Func<string, string?> Env(string? value)
            => name => name == CredentialsDirectory.EnvName ? value : null;

        [Fact]
        public void EnvName_IsTheContractTheDevStandSets()
            => Assert.Equal("UNITY_MCP_CREDENTIALS_DIR", CredentialsDirectory.EnvName);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Unset_MeansThePlatformDefault(string? value)
            => Assert.Null(CredentialsDirectory.Resolve(Env(value)));

        [Fact]
        public void AbsolutePath_IsUsed()
        {
            var dir = Path.Combine(Path.GetTempPath(), "unity-store", "..", "unity-store");
            Assert.Equal(Path.GetFullPath(dir), CredentialsDirectory.Resolve(Env(dir)));
        }

        [Theory]
        [InlineData("relative/store")]
        [InlineData(".ai-game-dev")]
        public void RelativePath_IsRejected(string value)
            => Assert.Throws<ArgumentException>(() => CredentialsDirectory.Resolve(Env(value)));

        [Fact]
        public void DriveRelativePath_IsRejectedOnWindows()
        {
            if (Path.DirectorySeparatorChar != '\\')
                return;
            Assert.Throws<ArgumentException>(() => CredentialsDirectory.Resolve(Env("C:store")));
            Assert.Throws<ArgumentException>(() => CredentialsDirectory.Resolve(Env(@"\store")));
            Assert.Equal(@"\\server\share\store", CredentialsDirectory.Resolve(Env(@"\\server\share\store")));
        }

        [Fact]
        public void ResolveOrDefault_IgnoresAnInvalidValue()
            => Assert.Null(CredentialsDirectory.ResolveOrDefault(null, Env("relative/store")));
    }
}
