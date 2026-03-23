namespace Entegrasyon.Business.Abstract;

public interface IPttavmShipmentApiClient
{
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
}
