using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Sales;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleManager
{
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    private readonly FluentValidator _fluentValidator;
    private readonly OfficeStockManager _officeStockManager;
    public SaleManager(ApplicationLogManager applicationLogManager, IMapper mapper, FluentValidator fluentValidator, OfficeStockManager officeStockManager)
    {
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _fluentValidator = fluentValidator;
        _officeStockManager = officeStockManager;
    }

    public async Task<IResult> MakeSale(MakeSaleDto dto)
    {
        await _applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        //business logic control
        var sale = new Sale()
        {
            CustomerId = dto.CustomerId,SalePersonId = dto.SalePersonId
        };
        var decreaseStockResult = await _officeStockManager.DecreaseProductsStock(dto.SaleItems.Select(x => 
            new DecreaseStockDto(x.ProductVariantId,x.Quantity)));
        if(!decreaseStockResult.Success)
            return decreaseStockResult;
        return new SuccessResult();
    }
    
    
}
