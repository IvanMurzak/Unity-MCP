// Cases: ref/out/in parameters
namespace com.IvanMurzak.Unity.MCP.Common.Tests.SampleData
{
    public class RefOutIn_Methods
    {
        public void Increment(ref int value)
        {
            value++;
        }

        public bool TryParseInt(string text, out int value)
        {
            return int.TryParse(text, out value);
        }

        public int Sum(in int a, in int b)
        {
            return a + b;
        }
    }
}
