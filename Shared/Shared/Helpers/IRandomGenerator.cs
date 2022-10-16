using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
    public interface IRandomGenerator 
    {
        string GetRandomCode(int length);
        string GetRandomCode(int length,bool includeNumbers,bool includeLower,bool includeUpper);
    }
}
