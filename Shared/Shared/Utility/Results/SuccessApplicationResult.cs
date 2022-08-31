namespace Shared.Utility.Results
{
    public class SuccessApplicationResult : ApplicationResult
    {
        public SuccessApplicationResult() : base(true)
        {
        }

        public SuccessApplicationResult(string message) : base(true,message)
        {
        }
    }
}