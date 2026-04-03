namespace Entegrasyon.Entity.Results
{
    [Obsolete("Use ResultRecord.Fail() instead. Will be removed in a future version.")]
    public class ErrorResult : Result
    {
        public ErrorResult() : base(false)
        {
        }

        public ErrorResult(string message) : base(false,message)
        {
        }
    }
}
