using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ITrendyolOrderService
{
    Task<IDataResult<List<TrendyolShipmentPackage>>> FetchOrdersAsync(TrendyolOrderQueryParams query);
    Task<IResult> MarkUnsuppliedAsync(long shipmentPackageId, List<long> lineIds);
    Task<IResult> UpdateTrackingNumberAsync(long shipmentPackageId, string trackingNumber);
    Task<IDataResult<byte[]>> GetShippingLabelAsync(long shipmentPackageId);
}
