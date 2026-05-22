using System.Collections.Generic;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.TestFiles
{
    public class DataFieldPopulationTestScriptableObject : ScriptableObject
    {
        public Sprite spriteField;
        public Material materialField;
        public GameObject gameObjectField;
        public Texture2D textureField;
        public ScriptableObject scriptableObjectField;
        public GameObject prefabField;

        public Material[] materialArray;
        public GameObject[] gameObjectArray;

        public List<Material> materialList;
        public List<GameObject> gameObjectList;

        public int intField;
        public string stringField;
    }
}
