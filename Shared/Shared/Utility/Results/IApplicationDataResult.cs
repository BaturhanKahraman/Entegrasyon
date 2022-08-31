namespace Shared.Utility.Results
{
    public interface IApplicationDataResult<out T> : IApplicationResult
    {
        public T Data { get; }
    }
}