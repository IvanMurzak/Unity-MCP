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

#if UNITY_EDITOR_WIN
        // Windows-only: the boundary case relies on `Path.GetFullPath("C:\\…")`
        // returning the input unchanged. Mono on Linux does not recognize the
        // `C:\` drive prefix and treats the whole input as a relative path,
        // prepending the cwd (e.g. `/github/workspace/Unity-Tests/<ver>/`). The
        // resolved path then exceeds the 255-char threshold and the pre-flight
        // throws — correctly for Windows semantics, but the test is asserting
        // exactly the Windows path-resolution shape, so we gate it accordingly.
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
#endif

#if UNITY_EDITOR_WIN
        // Same Windows-only rationale as the boundary-at-threshold test above:
        // Mono on Linux does not recognise `C:\` as a drive prefix, so
        // `Path.GetFullPath` prepends the cwd and inflates the resolved path
        // well past 255. The threshold-throws assertion still passes by
        // accident on Linux (because 256 + cwd-prefix is still > 255), but
        // that is meaningless — the test no longer exercises the boundary.
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
#endif

#if !UNITY_EDITOR_WIN
        // Unix counterpart of the boundary-at-threshold test. A `/`-rooted path
        // is already absolute on Unix, so `Path.GetFullPath` returns it unchanged
        // and the boundary check at <= 255 holds. Pins the same `<=` semantics
        // on Linux/macOS that the Windows variant pins on Windows.
        [Test]
        public void CheckWith_BoundaryAtThreshold_UnixShapedPath_DoesNotThrow()
        {
            var prefix = "/tmp/";
            var suffix = ".dll";
            var fillerLen = 255 - prefix.Length - suffix.Length;
            var path = prefix + new string('a', fillerLen) + suffix;
            Assert.AreEqual(255, path.Length);

            Assert.DoesNotThrow(() =>
                NuGetLongPathPreflight.CheckWith(path, "Pkg", isWindows: true, maxAllowedPathLength: 255));
        }

        // Unix counterpart of the boundary-above-threshold test. A `/`-rooted
        // 256-char path round-trips through `Path.GetFullPath` unchanged on
        // Unix, so the rejection fires for the right reason rather than via
        // cwd-prefix inflation.
        [Test]
        public void CheckWith_BoundaryAboveThreshold_UnixShapedPath_Throws()
        {
            var prefix = "/tmp/";
            var suffix = ".dll";
            var fillerLen = 256 - prefix.Length - suffix.Length;
            var path = prefix + new string('a', fillerLen) + suffix;
            Assert.AreEqual(256, path.Length);

            Assert.Throws<InstallPathTooLongException>(() =>
                NuGetLongPathPreflight.CheckWith(path, "Pkg", isWindows: true, maxAllowedPathLength: 255));
        }
#endif
    }
}
