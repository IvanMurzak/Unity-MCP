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
using System.Collections.Generic;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for the project-level NuGet skip list: the built-in and per-project lists
    /// merge into one case-insensitive set, and skipped packages the project does not
    /// provide are reported so the restore can warn about them by name.
    /// </summary>
    [TestFixture]
    public class NuGetSkipListTests
    {
        [Test]
        public void Merge_UnionsBuiltInAndProjectLists()
        {
            var merged = NuGetSkipList.Merge(
                new[] { "Microsoft.Bcl.Memory" },
                new[] { "Microsoft.AspNetCore.SignalR.Client" });

            Assert.That(merged, Is.EquivalentTo(new[] { "Microsoft.Bcl.Memory", "Microsoft.AspNetCore.SignalR.Client" }));
        }

        [Test]
        public void Merge_IsCaseInsensitive()
        {
            var merged = NuGetSkipList.Merge(
                new[] { "Microsoft.AspNetCore.SignalR.Client" },
                new[] { "microsoft.aspnetcore.signalr.client" });

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged.Contains("MICROSOFT.ASPNETCORE.SIGNALR.CLIENT"), Is.True);
        }

        [Test]
        public void Merge_TrimsEntriesAndDropsBlanks()
        {
            // The Project Settings page adds an empty row before the user types the ID;
            // a half-edited list must never skip "" or " ".
            var merged = NuGetSkipList.Merge(
                Array.Empty<string>(),
                new[] { "  Microsoft.AspNetCore.SignalR.Client ", "", "   " });

            Assert.That(merged, Is.EquivalentTo(new[] { "Microsoft.AspNetCore.SignalR.Client" }));
        }

        [Test]
        public void Merge_WithBothListsEmpty_IsEmpty()
        {
            var merged = NuGetSkipList.Merge(Array.Empty<string>(), Array.Empty<string>());

            Assert.That(merged, Is.Empty);
        }

        [Test]
        public void FindUnprovided_ReturnsOnlyPackagesTheProjectDoesNotProvide()
        {
            var provided = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft.AspNetCore.SignalR.Client"
            };

            var unprovided = NuGetSkipList.FindUnprovided(
                new[] { "Microsoft.AspNetCore.SignalR.Client", "Microsoft.AspNetCore.SignalR.Protocols.Json" },
                provided.Contains);

            Assert.That(unprovided, Is.EquivalentTo(new[] { "Microsoft.AspNetCore.SignalR.Protocols.Json" }));
        }

        [Test]
        public void FindUnprovided_WithEverythingProvided_IsEmpty()
        {
            var unprovided = NuGetSkipList.FindUnprovided(
                new[] { "Microsoft.AspNetCore.SignalR.Client" },
                _ => true);

            Assert.That(unprovided, Is.Empty);
        }
    }
}
