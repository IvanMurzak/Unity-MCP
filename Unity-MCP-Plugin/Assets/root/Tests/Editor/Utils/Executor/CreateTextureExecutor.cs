using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateTextureExecutor : BaseCreateAssetExecutor<Texture2D>
    {
        public CreateTextureExecutor(string assetName, params string[] folders) : base(assetName, folders)
        {
            SetAction(() =>
            {
                Debug.Log($"Creating Texture: {AssetPath}");

                var texture = new Texture2D(64, 64);
                // Fill with some color
                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        texture.SetPixel(x, y, Color.red);
                    }
                }
                texture.Apply();

                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(AssetPath, bytes);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Asset = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath);

                if (Asset == null)
                {
                    Debug.LogError($"Failed to load created texture at {AssetPath}");
                }
                else
                {
                    Debug.Log($"Created Texture: {AssetPath}");
                }

                return Asset;
            });
        }
    }
}
