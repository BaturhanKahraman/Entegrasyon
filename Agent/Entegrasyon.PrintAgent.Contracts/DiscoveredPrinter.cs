namespace Entegrasyon.PrintAgent.Contracts;

public record DiscoveredPrinter(string Name, string ConnectionType, string IpAddress, bool IsDefault);
