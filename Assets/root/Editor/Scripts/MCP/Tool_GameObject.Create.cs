using System;
using com.IvanMurzak.UnityMCP.Common.API;
using UnityEngine;

namespace com.IvanMurzak.UnityMCP.API.Editor
{
    [ToolType]
    public partial class Tool_GameObject
    {
        [Tool]
        public string Create(string path, string name)
        {
            try
            {
                var targetParent = string.IsNullOrEmpty(path) ? null : GameObject.Find(path);
                if (targetParent == null && string.IsNullOrEmpty(path))
                {
                    return $"[Error] Parent GameObject '{path}' not found.";
                }

                var go = new GameObject(name);
                go.transform.position = new Vector3(0, 0, 0);
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = new Vector3(1, 1, 1);
                if (targetParent != null)
                    go.transform.SetParent(targetParent.transform, false);

                return $"[Success] Created GameObject '{name}' at path '{path}'.";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Error] Failed to create GameObject: {ex.Message}");
                return ex.ToString();
            }
        }
    }
}