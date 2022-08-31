namespace Shared.Utility.Results
{
    public class ErrorApplicationDataApplicationResult<T> : ApplicationDataApplicationResult<T>
    {
        public ErrorApplicationDataApplicationResult(T data) : base(data,false)
        {
        }

        public ErrorApplicationDataApplicationResult(T data,string message) : base(data,false,message)
        {
        }
    }
}