namespace Entegrasyon.Entity.Tenants;

public class ConnectionInfo
{
    public string Database { get; set; }
    public string Host { get; set; }
    public int? Port { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public DatabaseType DatabaseType { get; set; }
}