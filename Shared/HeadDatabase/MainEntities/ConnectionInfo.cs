namespace MainDatabase.MainEntities;

public class ConnectionInfo
{
    public int Id { get; set; }
    public CloudEnviroment CloudEnviroment { get; set; }
    public string ApiKey { get; set; }
    public string SecretKey { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public string ConnectionString { get; set; }
    public DatabaseType DatabaseType { get; set; }
}