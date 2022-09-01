namespace Shared.Results
{
    public class SuccessApplicationDataApplicationResult<T> : ApplicationDataApplicationResult<T>
    {
        public SuccessApplicationDataApplicationResult(T data) : base(data,true)
        {
        }

        public SuccessApplicationDataApplicationResult(T data,string message) : base(data,true,message)
        {
        }
    }
}