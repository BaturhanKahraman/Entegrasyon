using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
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
    private readonly ISaleDal _saleDal;
    public SaleManager(ApplicationLogManager applicationLogManager, IMapper mapper, FluentValidator fluentValidator, OfficeStockManager officeStockManager, ISaleDal saleDal)
    {
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _fluentValidator = fluentValidator;
        _officeStockManager = officeStockManager;
        _saleDal = saleDal;
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
        if (!decreaseStockResult.Success)
        {
            //await _applicationLogManager.AddLog("Satış ", LogType.Sale, LogAction.Add, dto);
            return decreaseStockResult;
        }
        await _saleDal.AddAsync(sale);
        await _applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
        return new SuccessResult();
    }
    
    
}
