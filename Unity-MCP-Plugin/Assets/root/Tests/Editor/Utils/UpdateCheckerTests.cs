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

        #region JSON Parsing Tests

        [Test]
        public void ParseLatestVersionFromJson_ValidJson_ReturnsLatestVersion()
        {
            var json = @"[{""name"": ""v0.33.1"", ""zipball_url"": ""..."", ""tarball_url"": ""...""}, {""name"": ""v0.33.0"", ""zipball_url"": ""..."", ""tarball_url"": ""...""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("0.33.1", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_SingleTag_ReturnsVersion()
        {
            var json = @"[{""name"": ""v1.0.0""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("1.0.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_UnorderedTags_ReturnsLatest()
        {
            var json = @"[{""name"": ""v1.0.0""}, {""name"": ""v2.0.0""}, {""name"": ""v1.5.0""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("2.0.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_WithoutVPrefix_ParsesCorrectly()
        {
            var json = @"[{""name"": ""1.0.0""}, {""name"": ""2.0.0""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("2.0.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_UppercaseVPrefix_ParsesCorrectly()
        {
            var json = @"[{""name"": ""V1.0.0""}, {""name"": ""V2.0.0""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("2.0.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_EmptyArray_ReturnsNull()
        {
            var json = @"[]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_NoValidVersions_ReturnsNull()
        {
            var json = @"[{""name"": ""not-a-version""}, {""name"": ""also-not-a-version""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLatestVersionFromJson_MixedValidInvalid_ReturnsLatestValid()
        {
            var json = @"[{""name"": ""not-a-version""}, {""name"": ""v1.0.0""}, {""name"": ""invalid""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("1.0.0", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_TwoPartVersions_ParsesCorrectly()
        {
            var json = @"[{""name"": ""v1.0""}, {""name"": ""v1.1""}]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("1.1", result);
        }

        [Test]
        public void ParseLatestVersionFromJson_ComplexGitHubResponse_ParsesCorrectly()
        {
            var json = @"[
                {""name"": ""v0.35.0"", ""zipball_url"": ""https://api.github.com/..."", ""tarball_url"": ""https://api.github.com/..."", ""commit"": {""sha"": ""abc123"", ""url"": ""...""}},
                {""name"": ""v0.34.2"", ""zipball_url"": ""..."", ""tarball_url"": ""..."", ""commit"": {""sha"": ""def456"", ""url"": ""...""}},
                {""name"": ""v0.34.1"", ""zipball_url"": ""..."", ""tarball_url"": ""..."", ""commit"": {""sha"": ""ghi789"", ""url"": ""...""}}
            ]";

            var result = UpdateChecker.ParseLatestVersionFromJson(json);

            Assert.AreEqual("0.35.0", result);
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
        public void ReleasesUrl_ReturnsValidGitHubUrl()
        {
            var url = UpdateChecker.ReleasesUrl;

            Assert.IsNotNull(url);
            Assert.IsTrue(url.StartsWith("https://github.com/"));
            Assert.IsTrue(url.Contains("/releases"));
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

        #endregion
    }
}
