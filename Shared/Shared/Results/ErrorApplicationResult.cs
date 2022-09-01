namespace Shared.Results
{
    public class ErrorApplicationResult : ApplicationResult
    {
        public ErrorApplicationResult() : base(false)
        {
        }

        public ErrorApplicationResult(string message) : base(false,message)
        {
        }
    }
}