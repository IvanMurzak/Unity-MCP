// Private property
namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class Property_Private
    {
        private int Secret { get; set; }

        public void SetSecret(int value) => Secret = value;
        public int GetSecret() => Secret;
    }
}
