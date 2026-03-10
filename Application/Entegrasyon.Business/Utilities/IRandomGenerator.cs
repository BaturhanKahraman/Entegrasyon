namespace Entegrasyon.Business.Utilities;

public interface IRandomGenerator
{
    string GetRandomCode(int length,bool includeNumbers=true,bool includeLower=true,bool includeUpper=true);
}
