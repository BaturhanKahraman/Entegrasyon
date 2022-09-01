using System.Text.Json;

namespace Shared.Results
{
    public class ErrorDetail
    {
        public string Error { get; set; }

        public ErrorDetail(string errorDetail)
        {
            Error = errorDetail;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}