namespace Marketplace
{
    public interface IMarketPlaceProductService
    {
        Task AddProduct();
        Task DeleteProduct();
        Task UpdateStock();
        Task UpdateProduct();
        Task GetProducts();

    }
}