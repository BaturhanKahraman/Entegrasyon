namespace Shared.Utility.Results
{
    public class ApplicationDataApplicationResult<T> : ApplicationResult, IApplicationDataResult<T>
    {
        public T Data { get; }

        public ApplicationDataApplicationResult(T data,bool success) : base(success)
        {
            Data = data;
        }
        public ApplicationDataApplicationResult(T data,bool success,string message) : base(success,message)
        {
            Data = data;
        }
    }
}