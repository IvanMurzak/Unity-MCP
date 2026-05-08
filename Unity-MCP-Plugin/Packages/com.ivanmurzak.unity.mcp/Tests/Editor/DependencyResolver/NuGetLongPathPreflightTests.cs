/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+------------------------------------------------------------------+
*/

#nullable enable
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for the Windows MAX_PATH pre-flight check introduced for issue
    /// #733. The test seam <see cref="NuGetLongPathPreflight.CheckWith"/> lets
    /// us drive the rejection deterministically by injecting the OS check and
    /// the threshold instead of relying on an actual ~250-char temp path.
    /// </summary>
    [TestFixture]
    public class NuGetLongPathPreflightTests
    {
        [Test]
        public void CheckWith_NoOpOnNonWindows()
        {
            // 5000-char fake path — much larger than Windows' 260, but the
            // pre-flight is a no-op on macOS/Linux.
            var longPath = "/tmp/" + new string('x', 5000) + ".dll";
            Assert.DoesNotThrow(() =>
                NuGetLongPathPreflight.CheckWith(longPath, "Some.Package", isWindows: false, maxAllowedPathLength: 255));
        }

        [Test]
        public void CheckWith_ThrowsOnWindowsWhenPathExceedsThreshold()
        {
            // 300-char path with a Windows-shaped prefix; threshold 255.
            var longPath = "C:\\" + new string('x', 300) + ".dll";
            var ex = Assert.Throws<InstallPathTooLongException>(() =>
                NuGetLongPathPreflight.CheckWith(longPath, "Some.Package", isWindows: true, maxAllowedPathLength: 255));

            Assert.IsNotNull(ex);
            StringAssert.Contains("Some.Package", ex!.Message);
            StringAssert.Contains("260-character path limit", ex.Message);
            Assert.That(ex.PlannedPathLength, Is.GreaterThan(255));
        }

        [Test]
        public void CheckWith_ReturnsNormallyOnWindowsWhenPathFits()
        {
            // Short path, well below any reasonable threshold.
            var shortPath = "C:\\src\\proj\\Assets\\Plugins\\NuGet\\System.Memory.10.0.3.dll";
            Assert.DoesNotThrow(() =>
                NuGetLongPathPreflight.CheckWith(shortPath, "Microsoft.Bcl.Memory", isWindows: true, maxAllowedPathLength: 255));
        }

        [Test]
        public void CheckWith_BoundaryAtThreshold_DoesNotThrow()
        {
            // Path of exactly 255 characters at threshold 255 must NOT throw.
            // This pins the "<=" comparison so a future change to "<" (which
            // would reject the boundary) is caught by the test gate.
            var prefix = "C:\\";
            var suffix = ".dll";
            var fillerLen = 255 - prefix.Length - suffix.Length;
            var path = prefix + new string('a', fillerLen) + suffix;
            Assert.AreEqual(255, path.Length);

            Assert.DoesNotThrow(() =>
                NuGetLongPathPreflight.CheckWith(path, "Pkg", isWindows: true, maxAllowedPathLength: 255));
        }

        [Test]
        public void CheckWith_BoundaryAboveThreshold_Throws()
        {
            // Path of exactly 256 characters at threshold 255 MUST throw.
            var prefix = "C:\\";
            var suffix = ".dll";
            var fillerLen = 256 - prefix.Length - suffix.Length;
            var path = prefix + new string('a', fillerLen) + suffix;
            Assert.AreEqual(256, path.Length);

            Assert.Throws<InstallPathTooLongException>(() =>
                NuGetLongPathPreflight.CheckWith(path, "Pkg", isWindows: true, maxAllowedPathLength: 255));
        }
    }
}
