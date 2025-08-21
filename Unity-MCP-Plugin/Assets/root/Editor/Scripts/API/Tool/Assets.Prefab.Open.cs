#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.ComponentModel;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model.Unity;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        [McpPluginTool
        (
            "Assets_Prefab_Open",
            Title = "Open prefab"
        )]
        [Description(@"Open prefab edit mode for a specific GameObject. In the Edit mode you can modify the prefab.
The modification will be applied to the all instances of the prefab across the project.
Note: Please 'Close' the prefab later to exit prefab editing mode.")]
        public string Open
        (
            [Description("GameObject that represents prefab instance of an original prefab GameObject.")]
            GameObjectRef gameObjectRef
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRef?.IsValid == false)
                return $"[Error] '{nameof(gameObjectRef)}' is not valid. Please provide at least a single valid {string.Join(", ", $"'{AssetObjectRef.AssetObjectRefProperty.All}'")} property.";

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            var gameObject = gameObjectRef.FindGameObject();

            if (gameObject == null)
                return "[Error] GameObject not found. Provide a reference to existed GameObject.";

            var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

            prefabStage = gameObject.IsAsset()
                ? UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(prefabAssetPath)
                : UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(prefabAssetPath, gameObject);

            if (prefabStage == null)
                return Error.PrefabStageIsNotOpened();

            var name = typeof(Tool_Assets_Prefab)
                .GetMethod(nameof(Close))
                .GetCustomAttribute<McpPluginToolAttribute>()
                .Name;

            return @$"[Success] Prefab '{prefabStage.assetPath}' opened. Use '{name}' to close it.
# Prefab information:
{prefabStage.prefabContentsRoot.ToMetadata().Print()}";
        });
    }
}