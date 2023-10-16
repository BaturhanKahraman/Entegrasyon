namespace Shared.Helpers
{
    public sealed class RandomGenerator : IRandomGenerator
    {
        private const int CASE_NUMBER = 3;
        private static readonly Random Random = new();
        private static readonly int[] ZeroToNineAsci = Enumerable.Range(48,10).ToArray();
        private static readonly int[] AtoZLower = Enumerable.Range(65,26).ToArray();
        private static readonly int[] AtoZUpper = Enumerable.Range(97,26).ToArray();
     
        public string GetRandomCode(int length,bool includeNumbers=true,bool includeLower=true,bool includeUpper=true)
        {
            string result = string.Empty;
            List<int> asciList = new();
            if(includeNumbers)
                asciList.AddRange(ZeroToNineAsci);
            if(includeLower)
                asciList.AddRange(AtoZLower);
            if(includeUpper)
                asciList.AddRange(AtoZUpper);
            for(int i = 0;i < length;i++)
            {
                result += (char)asciList[Random.Next(asciList.Count)];
            }
            return result;
        }
    }
}
