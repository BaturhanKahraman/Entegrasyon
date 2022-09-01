namespace Shared.Results
{
    public interface IApplicationResult
    {
        bool Success { get; }
        string Message { get; }

    }
}