namespace Entegrasyon.PrintAgent.Configuration;

public class AgentConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public List<string> AllowedOrigins { get; set; } = [];
    public int Port { get; set; } = 19100;
}
