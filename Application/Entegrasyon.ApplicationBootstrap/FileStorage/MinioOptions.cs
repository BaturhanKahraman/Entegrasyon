namespace Entegrasyon.ApplicationBootstrap.FileStorage;

public class MinioOptions
{
    public string Endpoint { get; set; } = "192.168.1.78:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = false;
    public string BucketName { get; set; } = "products";
    public string PublicBaseUrl { get; set; } = "http://192.168.1.78:9000";
}
