namespace Entegrasyon.Business.Abstract;

public interface IBarcodeService
{
    Task<string> GenerateAsync();
}
