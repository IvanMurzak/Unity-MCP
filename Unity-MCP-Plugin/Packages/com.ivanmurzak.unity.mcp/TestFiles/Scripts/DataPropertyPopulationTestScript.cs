using System.Collections.Generic;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.TestFiles
{
    public class DataPropertyPopulationTestScript : MonoBehaviour
    {
        public Sprite spriteProperty { get; set; }
        public Material materialProperty { get; set; }
        public GameObject gameObjectProperty { get; set; }
        public Texture2D textureProperty { get; set; }
        public ScriptableObject scriptableObjectProperty { get; set; }
        public GameObject prefabProperty { get; set; }

        public Material[] materialArrayProperty { get; set; }
        public GameObject[] gameObjectArrayProperty { get; set; }

        public List<Material> materialListProperty { get; set; }
        public List<GameObject> gameObjectListProperty { get; set; }

        public int intProperty { get; set; }
        public string stringProperty { get; set; }
    }
}
