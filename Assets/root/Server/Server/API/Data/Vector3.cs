#if !UNITY_5_3_OR_NEWER
namespace com.IvanMurzak.Unity.MCP.Server.API.Data
{
    [System.Serializable]
    public class Vector3
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.z = 0f;
        }
        public Vector3(float x)
        {
            this.x = x;
            this.y = 0f;
            this.z = 0f;
        }
        public Vector3()
        {
            this.x = 0f;
            this.y = 0f;
            this.z = 0f;
        }
    }
}
#endif