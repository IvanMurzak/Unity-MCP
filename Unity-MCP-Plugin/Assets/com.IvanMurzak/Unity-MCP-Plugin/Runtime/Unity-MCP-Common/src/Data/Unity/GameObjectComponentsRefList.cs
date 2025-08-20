using System.Collections.Generic;
using System.ComponentModel;

namespace com.IvanMurzak.ReflectorNet.Model.Unity
{
    [System.Serializable]
    [Description(@"GameObject references array. Used to specify GameObjects in opened Prefab or in the active Scene.")]
    public class GameObjectComponentsRefList : List<GameObjectComponentsRef>
    {
        public GameObjectComponentsRefList() { }

        public GameObjectComponentsRefList(int capacity) : base(capacity) { }

        public GameObjectComponentsRefList(IEnumerable<GameObjectComponentsRef> collection) : base(collection) { }

        public override string ToString()
        {
            if (Count == 0)
                return "No GameObjects";

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.AppendLine($"GameObjects total amount: {Count}");

            for (int i = 0; i < Count; i++)
                stringBuilder.AppendLine($"GameObject[{i}] {this[i]}");

            return stringBuilder.ToString();
        }
    }
}