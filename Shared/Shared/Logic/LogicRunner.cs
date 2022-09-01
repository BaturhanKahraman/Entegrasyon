using Shared.Results;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Logic
{
    static class LogicRunner
    {
        public static IApplicationResult Run(params IApplicationResult[] results)
        {
            foreach(var result in results)
            {
                if(!result.Success)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
