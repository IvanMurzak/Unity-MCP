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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class AssetsFindBuiltInTests : BaseTest
    {
        [Test]
        public void FindBuiltIn_ReturnsAssets_WithoutFilters()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn();

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count > 0, "Should return at least one built-in asset");
            Assert.IsTrue(results.Count <= 10, "Default maxResults should limit to 10");
        }

        [Test]
        public void FindBuiltIn_RespectsMaxResults()
        {
            var tool = new Tool_Assets();
            var maxResults = 5;
            var results = tool.FindBuiltIn(maxResults: maxResults);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count <= maxResults, $"Should return at most {maxResults} assets");
        }

        [Test]
        public void FindBuiltIn_RespectsMaxResults_LargeValue()
        {
            var tool = new Tool_Assets();
            var maxResults = 100;
            var results = tool.FindBuiltIn(maxResults: maxResults);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count <= maxResults, $"Should return at most {maxResults} assets");
        }

        [Test]
        public void FindBuiltIn_ThrowsException_WhenMaxResultsIsZero()
        {
            var tool = new Tool_Assets();

            var ex = Assert.Throws<System.ArgumentException>(() =>
            {
                tool.FindBuiltIn(maxResults: 0);
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("maxResults"), "Exception should mention maxResults parameter");
        }

        [Test]
        public void FindBuiltIn_ThrowsException_WhenMaxResultsIsNegative()
        {
            var tool = new Tool_Assets();

            var ex = Assert.Throws<System.ArgumentException>(() =>
            {
                tool.FindBuiltIn(maxResults: -1);
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("maxResults"), "Exception should mention maxResults parameter");
        }

        [Test]
        public void FindBuiltIn_FiltersByType_Material()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(type: typeof(Material), maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsTrue(
                    result.AssetType == typeof(Material) || result.AssetType!.IsSubclassOf(typeof(Material)),
                    $"All results should be of type Material, but got {result.AssetType?.Name}");
            }
        }

        [Test]
        public void FindBuiltIn_FiltersByType_Shader()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(type: typeof(Shader), maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsTrue(
                    result.AssetType == typeof(Shader) || result.AssetType!.IsSubclassOf(typeof(Shader)),
                    $"All results should be of type Shader, but got {result.AssetType?.Name}");
            }
        }

        [Test]
        public void FindBuiltIn_FiltersByType_Texture()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(type: typeof(Texture), maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsTrue(
                    result.AssetType == typeof(Texture) || result.AssetType!.IsSubclassOf(typeof(Texture)),
                    $"All results should be of type Texture or subclass, but got {result.AssetType?.Name}");
            }
        }

        [Test]
        public void FindBuiltIn_FiltersByName_SingleWord()
        {
            var tool = new Tool_Assets();
            var searchName = "Default";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsTrue(
                    result.AssetPath!.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Asset path '{result.AssetPath}' should contain '{searchName}'");
            }
        }

        [Test]
        public void FindBuiltIn_FiltersByName_CaseInsensitive()
        {
            var tool = new Tool_Assets();
            var searchNameLower = "default";
            var searchNameUpper = "DEFAULT";

            var resultsLower = tool.FindBuiltIn(name: searchNameLower, maxResults: 50);
            var resultsUpper = tool.FindBuiltIn(name: searchNameUpper, maxResults: 50);

            Assert.IsNotNull(resultsLower, "Results (lowercase) should not be null");
            Assert.IsNotNull(resultsUpper, "Results (uppercase) should not be null");
            Assert.AreEqual(resultsLower.Count, resultsUpper.Count,
                "Case-insensitive search should return the same number of results");
        }

        [Test]
        public void FindBuiltIn_FiltersByName_MultipleWords()
        {
            var tool = new Tool_Assets();
            // Search with multiple words - should match if ANY word is found
            var searchName = "Default Sprite";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                var matchesDefault = result.AssetPath!.IndexOf("Default", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var matchesSprite = result.AssetPath!.IndexOf("Sprite", System.StringComparison.OrdinalIgnoreCase) >= 0;
                Assert.IsTrue(matchesDefault || matchesSprite,
                    $"Asset path '{result.AssetPath}' should contain 'Default' OR 'Sprite'");
            }
        }

        [Test]
        public void FindBuiltIn_CombinesNameAndTypeFilters()
        {
            var tool = new Tool_Assets();
            var searchName = "Default";
            var searchType = typeof(Material);
            var results = tool.FindBuiltIn(name: searchName, type: searchType, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsTrue(
                    result.AssetType == typeof(Material) || result.AssetType!.IsSubclassOf(typeof(Material)),
                    $"All results should be of type Material, but got {result.AssetType?.Name}");
                Assert.IsTrue(
                    result.AssetPath!.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Asset path '{result.AssetPath}' should contain '{searchName}'");
            }
        }

        [Test]
        public void FindBuiltIn_ReturnsNullGuidForBuiltInAssets()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(maxResults: 10);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsNull(result.AssetGuid,
                    $"Built-in asset '{result.AssetPath}' should have null GUID");
            }
        }

        [Test]
        public void FindBuiltIn_ReturnsCorrectAssetPath()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(maxResults: 10);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsNotNull(result.AssetPath, "AssetPath should not be null");
                Assert.IsTrue(
                    result.AssetPath!.StartsWith(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath),
                    $"Asset path '{result.AssetPath}' should start with '{ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath}'");
            }
        }

        [Test]
        public void FindBuiltIn_ReturnsUniqueAssets()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            var distinctPaths = results.Select(r => r.AssetPath).Distinct().Count();
            Assert.AreEqual(results.Count, distinctPaths, "All returned asset paths should be unique");
        }

        [Test]
        public void FindBuiltIn_ReturnsValidAssetType()
        {
            var tool = new Tool_Assets();
            var results = tool.FindBuiltIn(maxResults: 10);

            Assert.IsNotNull(results, "Results should not be null");
            foreach (var result in results)
            {
                Assert.IsNotNull(result.AssetType,
                    $"AssetType should not be null for asset '{result.AssetPath}'");
                Assert.IsTrue(
                    typeof(UnityEngine.Object).IsAssignableFrom(result.AssetType),
                    $"AssetType '{result.AssetType?.Name}' should be assignable to UnityEngine.Object");
            }
        }

        [Test]
        public void FindBuiltIn_FiltersByName_NoMatches_ReturnsEmptyList()
        {
            var tool = new Tool_Assets();
            var searchName = "NonExistentAssetNameXYZ123456";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.AreEqual(0, results.Count, "Should return empty list when no assets match the filter");
        }
    }
}
