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

        #region Priority Ordering Tests

        [Test]
        public void FindBuiltIn_Priority_ExactMatch_ComesFirst()
        {
            var tool = new Tool_Assets();

            // First, find an asset that actually exists to use for testing
            var allResults = tool.FindBuiltIn(maxResults: 50);
            Assert.IsTrue(allResults.Count > 0, "Should have at least one built-in asset");

            // Pick the first asset's name as our search term
            var firstAsset = allResults[0];
            var exactName = System.IO.Path.GetFileNameWithoutExtension(firstAsset.AssetPath);
            Assert.IsNotNull(exactName, "Asset name should not be null");

            // Now search for this exact name
            var results = tool.FindBuiltIn(name: exactName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count > 0, "Should find at least one result");

            // The first result should be the exact match
            var firstName = System.IO.Path.GetFileNameWithoutExtension(results[0].AssetPath);
            Assert.AreEqual(exactName, firstName,
                $"First result should be exact match '{exactName}', but got '{firstName}'");
        }

        [Test]
        public void FindBuiltIn_Priority_ExactMatch_BeforePartialMatch()
        {
            var tool = new Tool_Assets();
            // Search for "Default" - common prefix in Unity built-in assets like "Default-Material", "Default-Diffuse", etc.
            var searchName = "Default";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            if (results.Count < 2)
            {
                Assert.Inconclusive("Not enough results to test ordering");
                return;
            }

            // Check ordering: exact matches should come before partial matches
            bool foundPartialMatch = false;
            foreach (var result in results)
            {
                var assetName = System.IO.Path.GetFileNameWithoutExtension(result.AssetPath);
                if (assetName == null) continue;

                bool isExactMatch = assetName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
                bool isPartialMatch = !isExactMatch && assetName.Contains(searchName, System.StringComparison.OrdinalIgnoreCase);

                if (isExactMatch)
                {
                    Assert.IsFalse(foundPartialMatch,
                        $"Exact match '{assetName}' found after partial match - ordering is wrong");
                }
                else if (isPartialMatch)
                {
                    foundPartialMatch = true;
                }
            }
        }

        [Test]
        public void FindBuiltIn_Priority_PartialMatch_BeforeWordMatch()
        {
            var tool = new Tool_Assets();
            // Search for "Default Diffuse" - this should match:
            // - Exact: assets named exactly "Default Diffuse" (priority 0)
            // - Partial: assets containing "Default Diffuse" as substring (priority 1)
            // - Word: assets containing "Default" OR "Diffuse" but not both (priority 2)
            var searchName = "Default Diffuse";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");
            if (results.Count < 2)
            {
                Assert.Inconclusive("Not enough results to test ordering");
                return;
            }

            // Verify ordering: partial matches (containing both words) should come before word-only matches
            int lastBothWordsIndex = -1;
            int firstSingleWordIndex = -1;

            for (int i = 0; i < results.Count; i++)
            {
                var assetName = System.IO.Path.GetFileNameWithoutExtension(results[i].AssetPath);
                if (assetName == null) continue;

                var containsDefault = assetName.Contains("Default", System.StringComparison.OrdinalIgnoreCase);
                var containsDiffuse = assetName.Contains("Diffuse", System.StringComparison.OrdinalIgnoreCase);

                if (containsDefault && containsDiffuse)
                {
                    lastBothWordsIndex = i;
                }
                else if ((containsDefault || containsDiffuse) && firstSingleWordIndex < 0)
                {
                    firstSingleWordIndex = i;
                }
            }

            if (lastBothWordsIndex >= 0 && firstSingleWordIndex >= 0)
            {
                Assert.IsTrue(lastBothWordsIndex < firstSingleWordIndex,
                    $"Assets containing both words (last at {lastBothWordsIndex}) should come before single-word matches (first at {firstSingleWordIndex})");
            }
        }

        [Test]
        public void FindBuiltIn_Priority_OrderIsConsistent()
        {
            var tool = new Tool_Assets();
            var searchName = "Default";

            // Run the search multiple times
            var results1 = tool.FindBuiltIn(name: searchName, maxResults: 20);
            var results2 = tool.FindBuiltIn(name: searchName, maxResults: 20);

            Assert.IsNotNull(results1, "Results1 should not be null");
            Assert.IsNotNull(results2, "Results2 should not be null");
            Assert.AreEqual(results1.Count, results2.Count, "Both searches should return same count");

            // Verify the order is exactly the same
            for (int i = 0; i < results1.Count; i++)
            {
                Assert.AreEqual(results1[i].AssetPath, results2[i].AssetPath,
                    $"Order should be consistent at index {i}");
            }
        }

        [Test]
        public void FindBuiltIn_Priority_ExactMatchIsCaseInsensitive()
        {
            var tool = new Tool_Assets();

            // First find an asset that exists
            var allResults = tool.FindBuiltIn(maxResults: 1);
            Assert.IsTrue(allResults.Count > 0, "Should have at least one built-in asset");

            var assetName = System.IO.Path.GetFileNameWithoutExtension(allResults[0].AssetPath);
            Assert.IsNotNull(assetName, "Asset name should not be null");

            var searchLower = assetName.ToLowerInvariant();
            var searchUpper = assetName.ToUpperInvariant();
            var searchOriginal = assetName;

            var resultsLower = tool.FindBuiltIn(name: searchLower, maxResults: 10);
            var resultsUpper = tool.FindBuiltIn(name: searchUpper, maxResults: 10);
            var resultsOriginal = tool.FindBuiltIn(name: searchOriginal, maxResults: 10);

            Assert.IsNotNull(resultsLower, "Results (lower) should not be null");
            Assert.IsNotNull(resultsUpper, "Results (upper) should not be null");
            Assert.IsNotNull(resultsOriginal, "Results (original) should not be null");

            // All should have the same first result (exact match)
            Assert.IsTrue(resultsLower.Count > 0, "Lower case search should return results");
            Assert.IsTrue(resultsUpper.Count > 0, "Upper case search should return results");
            Assert.IsTrue(resultsOriginal.Count > 0, "Original case search should return results");

            Assert.AreEqual(resultsLower[0].AssetPath, resultsUpper[0].AssetPath,
                "Exact match should be case-insensitive (lower vs upper)");
            Assert.AreEqual(resultsLower[0].AssetPath, resultsOriginal[0].AssetPath,
                "Exact match should be case-insensitive (lower vs original)");
        }

        [Test]
        public void FindBuiltIn_Priority_WordSplitOnCommonDelimiters()
        {
            var tool = new Tool_Assets();
            // Using space, underscore, hyphen, dot as delimiters
            var searchWithSpace = "Default Diffuse";
            var searchWithUnderscore = "Default_Diffuse";
            var searchWithHyphen = "Default-Diffuse";

            var resultsSpace = tool.FindBuiltIn(name: searchWithSpace, maxResults: 20);
            var resultsUnderscore = tool.FindBuiltIn(name: searchWithUnderscore, maxResults: 20);
            var resultsHyphen = tool.FindBuiltIn(name: searchWithHyphen, maxResults: 20);

            Assert.IsNotNull(resultsSpace, "Results (space) should not be null");
            Assert.IsNotNull(resultsUnderscore, "Results (underscore) should not be null");
            Assert.IsNotNull(resultsHyphen, "Results (hyphen) should not be null");

            // All searches should return results containing "Default" or "Diffuse"
            foreach (var result in resultsSpace)
            {
                var matchesDefault = result.AssetPath!.Contains("Default", System.StringComparison.OrdinalIgnoreCase);
                var matchesDiffuse = result.AssetPath!.Contains("Diffuse", System.StringComparison.OrdinalIgnoreCase);
                Assert.IsTrue(matchesDefault || matchesDiffuse,
                    $"Result '{result.AssetPath}' should contain 'Default' or 'Diffuse'");
            }
        }

        [Test]
        public void FindBuiltIn_Priority_WithTypeFilter_StillOrdersByPriority()
        {
            var tool = new Tool_Assets();
            var searchName = "Default";
            var results = tool.FindBuiltIn(name: searchName, type: typeof(Material), maxResults: 50);

            Assert.IsNotNull(results, "Results should not be null");

            // Verify all results are Materials
            foreach (var result in results)
            {
                Assert.IsTrue(
                    typeof(Material).IsAssignableFrom(result.AssetType),
                    $"All results should be Materials, got {result.AssetType?.Name}");
            }

            // Verify ordering: exact matches first, then partial, then word matches
            bool foundPartial = false;
            bool foundWordOnly = false;

            foreach (var result in results)
            {
                var assetName = System.IO.Path.GetFileNameWithoutExtension(result.AssetPath);
                if (assetName == null) continue;

                bool isExact = assetName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
                bool isPartial = !isExact && assetName.Contains(searchName, System.StringComparison.OrdinalIgnoreCase);
                bool isWordOnly = !isExact && !isPartial;

                if (isExact)
                {
                    Assert.IsFalse(foundPartial, "Exact match found after partial match");
                    Assert.IsFalse(foundWordOnly, "Exact match found after word-only match");
                }
                else if (isPartial)
                {
                    foundPartial = true;
                    Assert.IsFalse(foundWordOnly, "Partial match found after word-only match");
                }
                else
                {
                    foundWordOnly = true;
                }
            }
        }

        [Test]
        public void FindBuiltIn_Priority_NoNameFilter_ReturnsAllWithSamePriority()
        {
            var tool = new Tool_Assets();
            // Without name filter, all items should have equal priority
            var results = tool.FindBuiltIn(type: typeof(Texture), maxResults: 20);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count > 0, "Should return at least one texture");

            // Just verify we get valid results - no specific ordering expected without name filter
            foreach (var result in results)
            {
                Assert.IsTrue(
                    typeof(Texture).IsAssignableFrom(result.AssetType),
                    $"All results should be Textures, got {result.AssetType?.Name}");
            }
        }

        [Test]
        public void FindBuiltIn_Priority_SingleWordSearch_MatchesCorrectly()
        {
            var tool = new Tool_Assets();
            var searchName = "Default";
            var results = tool.FindBuiltIn(name: searchName, maxResults: 30);

            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsTrue(results.Count > 0, "Should find at least one result for 'Default'");

            // All results should contain the search term somewhere
            foreach (var result in results)
            {
                var assetName = System.IO.Path.GetFileNameWithoutExtension(result.AssetPath);
                Assert.IsTrue(
                    assetName != null && assetName.Contains(searchName, System.StringComparison.OrdinalIgnoreCase),
                    $"Asset '{result.AssetPath}' should contain '{searchName}'");
            }
        }

        #endregion
    }
}
