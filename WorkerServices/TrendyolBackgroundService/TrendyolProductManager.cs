using Marketplace;
using Microsoft.Extensions.Logging;

namespace TrendyolBackgroundJob
{
    public class TrendyolProductManager:IMarketPlaceProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrendyolProductManager> _logger;
        public TrendyolProductManager(HttpClient client, ILogger<TrendyolProductManager> logger)
        {
            _httpClient = client;
            _logger = logger;
        }
        public Task AddProduct()
        {
            throw new NotImplementedException();
        }

        public Task DeleteProduct()
        {
            throw new NotImplementedException();
        }

        public Task GetProducts()
        {
            throw new NotImplementedException();
        }

        public Task UpdateProduct()
        {
            throw new NotImplementedException();
        }

        public Task UpdateStock()
        {
            throw new NotImplementedException();
        }
    }
}
