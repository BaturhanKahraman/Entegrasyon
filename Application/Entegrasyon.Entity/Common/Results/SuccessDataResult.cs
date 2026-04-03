namespace Entegrasyon.Entity.Results
{
    [Obsolete("Use Result<T>.Ok() instead. Will be removed in a future version.")]
    public class SuccessDataResult<T> : DataResult<T>
    {
        public SuccessDataResult(T data) : base(data,true)
        {
        }

        public SuccessDataResult(T data,string message) : base(data,true,message)
        {
        }
    }
}
