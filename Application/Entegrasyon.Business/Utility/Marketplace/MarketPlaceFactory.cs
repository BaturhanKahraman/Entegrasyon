using Entegrasyon.Entity;
using Marketplace;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Business.Utility.Marketplace
{
    public class MarketPlaceServiceFactory
    {
        public IMarketPlaceProductService CreateMarketPlaceProductService(MarketPlace marketPlace)
        {

            throw new NotImplementedException();
        }
        public IMarketPlaceProductService CreateMarketPlaceProductService(IEnumerable<MarketPlace> marketPlaces)
        {
            throw new NotImplementedException();
        }
    }
   
}
