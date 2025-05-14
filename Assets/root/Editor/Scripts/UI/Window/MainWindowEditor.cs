using com.IvanMurzak.Unity.MCP.Utils;
using R3;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public partial class MainWindowEditor : EditorWindow
    {
        readonly CompositeDisposable _disposables = new();

        public static MainWindowEditor ShowWindow()
        {
            var window = GetWindow<MainWindowEditor>();
            window.titleContent = new GUIContent(text: "AI Connector");
            window.Focus();

            return window;
        }
        public static void ShowWindowVoid() => ShowWindow();

        public void Invalidate() => CreateGUI();
        void OnValidate() => McpPluginUnity.Validate();

        private void SaveChanges(string message)
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Info))
                Debug.Log(message);

            saveChangesMessage = message;

            Undo.RecordObject(McpPluginUnity.AssetFile, message); // Undo record started
            base.SaveChanges();
            McpPluginUnity.Save();
            McpPluginUnity.InvalidateAssetFile();
            EditorUtility.SetDirty(McpPluginUnity.AssetFile); // Undo record completed
        }

        private void OnChanged(McpPluginUnity.Data data) => Repaint();

        private void OnEnable()
        {
            McpPluginUnity.SubscribeOnChanged(OnChanged);
        }
        private void OnDisable()
        {
            McpPluginUnity.UnsubscribeOnChanged(OnChanged);
            _disposables.Clear();
        }
    }
}