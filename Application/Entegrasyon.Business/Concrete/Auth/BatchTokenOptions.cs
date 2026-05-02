namespace Entegrasyon.Business.Concrete.Auth;

public class BatchTokenOptions
{
    public const string SectionName = "BatchToken";
    public string SigningKey { get; set; } = string.Empty;
}
