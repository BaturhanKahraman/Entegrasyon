namespace Entegrasyon.Entity.Results
{
    [Obsolete("Use ResultRecord.Ok() instead. Will be removed in a future version.")]
    public class SuccessResult : Result
    {
        public SuccessResult() : base(true)
        {
        }

        public SuccessResult(string message) : base(true,message)
        {
        }
    }
}
