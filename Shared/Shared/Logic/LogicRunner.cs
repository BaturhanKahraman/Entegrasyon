using Shared.Results;

namespace Shared.Logic
{
    public static class LogicRunner
    {
        public static IResult Run(params ValueTuple<IResult,int>[] logics)
        {
            var results=logics.OrderBy(x => x.Item2).Select(x=>x.Item1).ToArray();
            return Run(results);
        }
        public static IResult Run(params IResult[] results)
        {
            foreach (var result in results)
            {
                if (!result.Success)
                    return result;
            }
            return null;
        }
        
    }
}
