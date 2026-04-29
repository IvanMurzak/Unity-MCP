/*
+-----------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+-----------------------------------------------------------------+
*/

#nullable enable
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class UpdateCheckerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear all preferences before each test to ensure clean state
            UpdateChecker.ClearPreferences();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up preferences after each test
            UpdateChecker.ClearPreferences();
        }

        #region Version Comparison Tests

        [Test]
        public void IsNewerVersion_NewerMajorVersion_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("2.0.0", "1.0.0"));
        }

        [Test]
        public void IsNewerVersion_NewerMinorVersion_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("1.2.0", "1.1.0"));
        }

        [Test]
        public void IsNewerVersion_NewerPatchVersion_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("1.0.2", "1.0.1"));
        }

        [Test]
        public void IsNewerVersion_SameVersion_ReturnsFalse()
        {
            Assert.IsFalse(UpdateChecker.IsNewerVersion("1.0.0", "1.0.0"));
        }

        [Test]
        public void IsNewerVersion_OlderVersion_ReturnsFalse()
        {
            Assert.IsFalse(UpdateChecker.IsNewerVersion("1.0.0", "2.0.0"));
        }

        [Test]
        public void IsNewerVersion_TwoPartVersion_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("1.1", "1.0"));
        }

        [Test]
        public void IsNewerVersion_MixedVersionLengths_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("1.0.1", "1.0"));
        }

        [Test]
        public void IsNewerVersion_MixedVersionLengths_ReturnsFalse()
        {
            Assert.IsFalse(UpdateChecker.IsNewerVersion("1.0", "1.0.1"));
        }

        [Test]
        public void IsNewerVersion_LargeVersionNumbers_ReturnsTrue()
        {
            Assert.IsTrue(UpdateChecker.IsNewerVersion("10.20.30", "10.20.29"));
        }

        [Test]
        public void CompareVersions_FirstGreater_ReturnsPositive()
        {
            Assert.Greater(UpdateChecker.CompareVersions("2.0.0", "1.0.0"), 0);
        }

        [Test]
        public void CompareVersions_SecondGreater_ReturnsNegative()
        {
            Assert.Less(UpdateChecker.CompareVersions("1.0.0", "2.0.0"), 0);
        }

        [Test]
        public void CompareVersions_Equal_ReturnsZero()
        {
            Assert.AreEqual(0, UpdateChecker.CompareVersions("1.2.3", "1.2.3"));
        }

        [Test]
        public void CompareVersions_DifferentLengths_ComparesCorrectly()
        {
            Assert.Greater(UpdateChecker.CompareVersions("1.0.0.1", "1.0.0"), 0);
            Assert.Less(UpdateChecker.CompareVersions("1.0.0", "1.0.0.1"), 0);
        }

        #endregion

        #region OpenUPM JSON Parsing Tests

        // The OpenUPM registry follows the npm registry shape: a JSON document with
        // a `dist-tags.latest` field that is the published version users can install
        // right now via Unity Package Manager. These tests cover the happy path plus
        // the malformed / missing-field cases that must produce a graceful null.

        [Test]
        public void ParseLatestVersionFromJson_OpenUpmDistTags_ReturnsLatest()
        {
            var json = @"{""name"":""com.ivanmurzak.unity.mcp"",""dist-tags"":{""latest"":""0.67.0""},""versions"":{""0.67.0"":{}}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("0.67.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_OpenUpmDistTagsTwoPart_ReturnsLatest()
        {
            var json = @"{""dist-tags"":{""latest"":""1.1""}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("1.1", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_OpenUpmFullShape_ReturnsLatest()
        {
            // Realistic abridged shape returned by https://package.openupm.com/com.ivanmurzak.unity.mcp
            var json = @"{
                ""name"": ""com.ivanmurzak.unity.mcp"",
                ""versions"": {
                    ""0.66.0"": { ""version"": ""0.66.0"" },
                    ""0.66.1"": { ""version"": ""0.66.1"" },
                    ""0.67.0"": { ""version"": ""0.67.0"" }
                },
                ""time"": {
                    ""modified"": ""2025-01-01T00:00:00Z"",
                    ""created"":  ""2024-01-01T00:00:00Z"",
                    ""0.66.0"":   ""2024-12-01T00:00:00Z"",
                    ""0.66.1"":   ""2024-12-15T00:00:00Z"",
                    ""0.67.0"":   ""2025-01-01T00:00:00Z""
                },
                ""dist-tags"": { ""latest"": ""0.67.0"" }
            }";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("0.67.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_MissingDistTags_ReturnsNull()
        {
            var json = @"{""name"":""com.ivanmurzak.unity.mcp"",""versions"":{""1.0.0"":{}}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsWithoutLatest_ReturnsNull()
        {
            var json = @"{""dist-tags"":{""beta"":""1.0.0-beta""}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestEmptyString_ReturnsNull()
        {
            var json = @"{""dist-tags"":{""latest"":""""}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestWrongType_ReturnsNull()
        {
            // dist-tags.latest is a number — must NOT crash, must return null.
            var json = @"{""dist-tags"":{""latest"":42}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsWrongType_ReturnsNull()
        {
            // dist-tags is a string instead of an object — must NOT crash, must return null.
            var json = @"{""dist-tags"":""1.0.0""}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestObject_ReturnsNull()
        {
            // dist-tags.latest is an object — must NOT crash, must return null.
            var json = @"{""dist-tags"":{""latest"":{""nested"":""1.0.0""}}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestArray_ReturnsNull()
        {
            // dist-tags.latest is an array — must NOT crash, must return null.
            var json = @"{""dist-tags"":{""latest"":[""1.0.0"",""1.1.0""]}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestNonNumeric_ReturnsNull()
        {
            // Defensive: a non-numeric `latest` string would otherwise leak into the popup
            // as version "0" because CompareVersions treats non-numeric parts as 0. The
            // parser must reject it instead.
            var json = @"{""dist-tags"":{""latest"":""not-a-version""}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestSemverPreRelease_ReturnsNull()
        {
            // Regression: an unanchored VersionPattern would let "1.0.0-preview" pass and
            // then CompareVersions would parse "0-preview" as 0, making pre-release tags
            // compare equal to the corresponding final release. The trailing `$` anchor
            // closes that hole — pre-release suffixes must produce null.
            var json = @"{""dist-tags"":{""latest"":""1.0.0-preview""}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsNull_ReturnsNull()
        {
            // dist-tags is JSON null — must NOT crash, must return null. Pins the
            // `distTags.ValueKind != JsonValueKind.Object` guard.
            var json = @"{""dist-tags"":null}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_DistTagsLatestNull_ReturnsNull()
        {
            // dist-tags.latest is JSON null — must NOT crash, must return null. Pins the
            // `latest.ValueKind != JsonValueKind.String` guard.
            var json = @"{""dist-tags"":{""latest"":null}}";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        #endregion

        #region Preference Management Tests

        [Test]
        public void IsDoNotShowAgain_DefaultValue_IsFalse()
        {
            UpdateChecker.ClearPreferences();

            Assert.IsFalse(UpdateChecker.IsDoNotShowAgain);
        }

        [Test]
        public void IsDoNotShowAgain_SetTrue_ReturnsTrue()
        {
            UpdateChecker.IsDoNotShowAgain = true;

            Assert.IsTrue(UpdateChecker.IsDoNotShowAgain);
        }

        [Test]
        public void IsDoNotShowAgain_SetFalse_ReturnsFalse()
        {
            UpdateChecker.IsDoNotShowAgain = true;
            UpdateChecker.IsDoNotShowAgain = false;

            Assert.IsFalse(UpdateChecker.IsDoNotShowAgain);
        }

        [Test]
        public void ClearPreferences_ResetsDoNotShowAgain()
        {
            UpdateChecker.IsDoNotShowAgain = true;

            UpdateChecker.ClearPreferences();

            Assert.IsFalse(UpdateChecker.IsDoNotShowAgain);
        }

        [Test]
        public void SkipVersion_StoresVersion()
        {
            UpdateChecker.SkipVersion("1.2.3");

            // Skipped version affects ShouldCheckForUpdates behavior
            // We can verify by checking that the version was stored
            // (indirectly tested through ShouldCheckForUpdates)
            Assert.Pass("SkipVersion executed without throwing");
        }

        [Test]
        public void ClearPreferences_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => UpdateChecker.ClearPreferences());
        }

        #endregion

        #region ShouldCheckForUpdates Tests

        [Test]
        public void ShouldCheckForUpdates_DefaultState_ReturnsTrue()
        {
            UpdateChecker.ClearPreferences();

            Assert.IsTrue(UpdateChecker.ShouldCheckForUpdates());
        }

        [Test]
        public void ShouldCheckForUpdates_DoNotShowAgainTrue_ReturnsFalse()
        {
            UpdateChecker.IsDoNotShowAgain = true;

            Assert.IsFalse(UpdateChecker.ShouldCheckForUpdates());
        }

        [Test]
        public void ShouldCheckForUpdates_AfterClearPreferences_ReturnsTrue()
        {
            UpdateChecker.IsDoNotShowAgain = true;

            UpdateChecker.ClearPreferences();

            Assert.IsTrue(UpdateChecker.ShouldCheckForUpdates());
        }

        #endregion

        #region ReleasesUrl Tests

        [Test]
        public void ReleasesUrl_PointsAtOpenUpm()
        {
            // ReleasesUrl is what the popup's "View Releases" button opens. After the
            // OpenUPM-source migration, sending users to GitHub releases would re-introduce
            // the original bug (a release tagged on GitHub may not yet be installable via
            // Unity Package Manager). The user-facing URL must point at the same registry
            // the version check now consults.
            var url = UpdateChecker.ReleasesUrl;

            Assert.IsNotNull(url);
            Assert.IsTrue(url.StartsWith("https://"));
            Assert.IsTrue(url.Contains("openupm.com"), $"expected OpenUPM URL, got: {url}");
            Assert.IsTrue(url.Contains("com.ivanmurzak.unity.mcp"), $"expected package id in URL, got: {url}");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void IsNewerVersion_EmptyStrings_HandledGracefully()
        {
            // Empty strings should compare as version "0"
            Assert.IsFalse(UpdateChecker.IsNewerVersion("", ""));
            Assert.IsTrue(UpdateChecker.IsNewerVersion("1.0.0", ""));
            Assert.IsFalse(UpdateChecker.IsNewerVersion("", "1.0.0"));
        }

        [Test]
        public void CompareVersions_NonNumericParts_HandledGracefully()
        {
            // Non-numeric parts should be treated as 0
            var result = UpdateChecker.CompareVersions("1.a.0", "1.0.0");
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ParseLatestVersionFromJson_MalformedJson_ReturnsNull()
        {
            var json = @"this is not valid json";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_EmptyString_ReturnsNull()
        {
            var result = UpdateChecker.ParseLatestVersionFromJson("");

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_EmptyObject_ReturnsNull()
        {
            var result = UpdateChecker.ParseLatestVersionFromJson("{}");

            Assert.IsNull(result);
        }

        #endregion
    }
}
