using System.Text.Json;

namespace Shared.Utility.Results
{
    public class ApplicationResult : IApplicationResult
    {
        public bool Success { get; }
        public string Message { get; }

        public ApplicationResult(bool success)
        {
            Success = success;
        }

        public ApplicationResult(bool success,string message) : this(success)
        {
            Message = message;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}