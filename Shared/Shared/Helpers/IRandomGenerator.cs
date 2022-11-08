namespace Shared.Helpers
{
    public interface IRandomGenerator 
    {
        string GetRandomCode(int length);
        string GetRandomCode(int length,bool includeNumbers,bool includeLower,bool includeUpper);
    }
}
