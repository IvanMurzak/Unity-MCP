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
using System.Collections;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        [UnityTest]
        public IEnumerator ComponentListAll_DefaultPagination_ReturnsFirstPage()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll();

            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.Items, "Items should not be null");
            Assert.AreEqual(0, result.Page, "Default page should be 0");
            Assert.AreEqual(5, result.PageSize, "Default page size should be 5");
            Assert.LessOrEqual(result.Items.Length, 5, "Items count should not exceed page size");
            Assert.GreaterOrEqual(result.TotalCount, result.Items.Length, "TotalCount should be >= items returned");
            Assert.GreaterOrEqual(result.TotalPages, 1, "TotalPages should be at least 1");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_CustomPageSize_ReturnsCorrectCount()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(pageSize: 10);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Page, "Page should be 0");
            Assert.AreEqual(10, result.PageSize, "Page size should be 10");
            Assert.LessOrEqual(result.Items.Length, 10, "Items count should not exceed page size");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_SecondPage_ReturnsDifferentItems()
        {
            var tool = new Tool_GameObject();

            var firstPage = tool.ListAll(page: 0, pageSize: 5);
            var secondPage = tool.ListAll(page: 1, pageSize: 5);

            Assert.IsNotNull(firstPage, "First page result should not be null");
            Assert.IsNotNull(secondPage, "Second page result should not be null");
            Assert.AreEqual(0, firstPage.Page, "First page should be 0");
            Assert.AreEqual(1, secondPage.Page, "Second page should be 1");

            // If there are enough items, second page should have different items
            if (firstPage.TotalCount > 5 && secondPage.Items.Length > 0)
            {
                Assert.AreNotEqual(firstPage.Items[0], secondPage.Items[0],
                    "First item of second page should be different from first page");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_SearchFilter_ReturnsFilteredResults()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(search: "Transform");

            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.Items, "Items should not be null");

            foreach (var item in result.Items)
            {
                Assert.IsTrue(item.ToLower().Contains("transform"),
                    $"Item '{item}' should contain 'transform'");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_SearchWithPagination_WorksTogether()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(search: "Renderer", page: 0, pageSize: 3);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Page, "Page should be 0");
            Assert.AreEqual(3, result.PageSize, "Page size should be 3");
            Assert.LessOrEqual(result.Items.Length, 3, "Items count should not exceed page size");

            foreach (var item in result.Items)
            {
                Assert.IsTrue(item.ToLower().Contains("renderer"),
                    $"Item '{item}' should contain 'renderer'");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_NegativePage_ClampedToZero()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(page: -5);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Page, "Negative page should be clamped to 0");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_PageSizeTooSmall_ClampedToOne()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(pageSize: 0);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(1, result.PageSize, "PageSize of 0 should be clamped to 1");
            Assert.LessOrEqual(result.Items.Length, 1, "Items count should not exceed clamped page size");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_PageSizeTooLarge_ClampedToMax()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(pageSize: 1000);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(500, result.PageSize, "PageSize of 1000 should be clamped to 500");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_PageBeyondTotal_ReturnsEmptyItems()
        {
            var tool = new Tool_GameObject();

            // First get total pages
            var firstResult = tool.ListAll(pageSize: 100);
            var beyondPage = firstResult.TotalPages + 10;

            var result = tool.ListAll(page: beyondPage, pageSize: 100);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(beyondPage, result.Page, "Page should match requested page");
            Assert.AreEqual(0, result.Items.Length, "Items should be empty for page beyond total");
            Assert.AreEqual(firstResult.TotalCount, result.TotalCount, "TotalCount should remain the same");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_TotalPagesCalculation_IsCorrect()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(pageSize: 10);

            var expectedTotalPages = (int)System.Math.Ceiling((double)result.TotalCount / result.PageSize);
            Assert.AreEqual(expectedTotalPages, result.TotalPages,
                $"TotalPages should be ceiling of TotalCount/PageSize ({result.TotalCount}/{result.PageSize})");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_CaseInsensitiveSearch_Works()
        {
            var tool = new Tool_GameObject();

            var lowerResult = tool.ListAll(search: "meshrenderer");
            var upperResult = tool.ListAll(search: "MESHRENDERER");
            var mixedResult = tool.ListAll(search: "MeshRenderer");

            Assert.AreEqual(lowerResult.TotalCount, upperResult.TotalCount,
                "Case insensitive search should return same count for lower and upper case");
            Assert.AreEqual(lowerResult.TotalCount, mixedResult.TotalCount,
                "Case insensitive search should return same count for mixed case");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_NoMatchingSearch_ReturnsEmptyWithZeroTotal()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(search: "XYZ_NonExistent_Component_12345");

            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Items.Length, "Items should be empty for non-matching search");
            Assert.AreEqual(0, result.TotalCount, "TotalCount should be 0 for non-matching search");
            Assert.AreEqual(0, result.TotalPages, "TotalPages should be 0 for non-matching search");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentListAll_ResultContainsKnownComponents()
        {
            var tool = new Tool_GameObject();

            var result = tool.ListAll(search: "Transform", pageSize: 100);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.Greater(result.TotalCount, 0, "Should find at least one Transform-related component");

            // UnityEngine.Transform should always exist
            var hasTransform = false;
            foreach (var item in result.Items)
            {
                if (item.Contains("Transform"))
                {
                    hasTransform = true;
                    break;
                }
            }
            Assert.IsTrue(hasTransform, "Should contain Transform component type");

            yield return null;
        }
    }
}
