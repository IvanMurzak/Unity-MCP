#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public abstract class BaseCreateAssetExecutor<T> : CreateFolderExecutor
    {
        protected readonly string _assetName;

        public T? Asset { get; protected set; }
        public string AssetPath => $"{FolderPath}/{_assetName}";

        public BaseCreateAssetExecutor(string assetName, params string[] folders) : base(folders)
        {
            _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        }

        protected override void PostExecute(object? input)
        {
            Debug.Log($"Deleting asset: {AssetPath}");
            AssetDatabase.DeleteAsset(AssetPath);
            AssetDatabase.Refresh();
            base.PostExecute(input);
        }
    }
}