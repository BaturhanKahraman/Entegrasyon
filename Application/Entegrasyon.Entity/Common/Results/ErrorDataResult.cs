namespace Entegrasyon.Entity.Results
{
    [Obsolete("Use Result<T>.Fail() instead. Will be removed in a future version.")]
    public class ErrorDataResult<T> : DataResult<T>
    {
        public ErrorDataResult(T data) : base(data,false)
        {
        }

        public ErrorDataResult(T data,string message) : base(data,false,message)
        {
        }
    }
}
