using com.IvanMurzak.ReflectorNet;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        const string GO_ParentName = "root 1";
        const string GO_Child1Name = "child 1";
        const string GO_Child2Name = "child 2";

        void ResultValidation(string result)
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] Result:\n{result}");
            Assert.IsNotNull(result, $"Result should not be empty or null.");
            Assert.IsTrue(result.ToLower().Contains("success"), $"Result should contain 'success'.\n{result}");
            Assert.IsFalse(result.ToLower().Contains("error"), $"Result should not contain 'error'.\n{result}");
            Assert.IsFalse(result.ToLower().Contains("warning"), $"Result should not contain 'warning'.\n{result}");
        }
    }
}