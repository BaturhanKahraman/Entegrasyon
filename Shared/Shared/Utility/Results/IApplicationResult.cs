namespace Shared.Utility.Results
{
    public interface IApplicationResult
    {
        bool Success { get; }
        string Message { get; }

    }
}