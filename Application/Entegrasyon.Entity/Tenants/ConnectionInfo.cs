namespace Entegrasyon.Entity.Tenants;

public class ConnectionInfo
{
    public string Database { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int? Port { get; set; }
    public string Password { get; set; } = null!;
    public string Username { get; set; } = null!;
    public DatabaseType DatabaseType { get; set; }
}