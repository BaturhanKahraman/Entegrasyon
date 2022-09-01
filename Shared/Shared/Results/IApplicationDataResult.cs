namespace Shared.Results
{
    public interface IApplicationDataResult<out T> : IApplicationResult
    {
        public T Data { get; }
    }
}