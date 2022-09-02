using System;
using System.Collections.Generic;
using System.Linq;
namespace Shared.Helpers
{
    public class RandomHelper : IRandomHelper
    {
        private static readonly Random _random = new();
        private static readonly int[] ZeroToNineAsci = Enumerable.Range(48,10).ToArray();
        private static readonly int[] AtoZLower = Enumerable.Range(65,26).ToArray();
        private static readonly int[] AtoZUpper = Enumerable.Range(97,26).ToArray();
        public string GetRandomCode(int length)
        {
            string result = string.Empty;
            for(int i = 0;i < length;i++)
            {
                int selector = _random.Next(3);
                switch(selector)
                {
                    case 0:
                        result += (char)ZeroToNineAsci[_random.Next(ZeroToNineAsci.Length)];
                        break;
                    case 1:
                        result += (char)AtoZLower[_random.Next(AtoZLower.Length)];
                        break;
                    case 2:
                        result += (char)AtoZUpper[_random.Next(AtoZUpper.Length)];
                        break;
                }
            }
            return result;
        }
    }
}
