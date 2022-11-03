using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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