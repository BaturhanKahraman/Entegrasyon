using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using Entegrasyon.Business.Mappers;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator fluentValidator,
    CustomerMapper mapper,
    IApplicationLogManager applicationLogManager) : ICustomerManager
{
    private const string EntityTypeCustomer = "Customer";

    public async Task<IDataResult<Customer>> UpdateCustomer(UpdateCustomerDto customerDto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog(
            "Müşteri düzenleme isteği geldi.", LogType.Customer, LogAction.Update,
            EntityTypeCustomer, customerDto.Id.ToString(), customerDto);
        Customer applicationCustomer = (await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == customerDto.Id))!;
        if (customerDto.CustomerType == "Retail")
        {
            if (applicationCustomer is CorporateCustomer corpCustomer)
            {
                corpCustomer.CorporateName = null;
                corpCustomer.TaxNumber = null;
            }
            var retailCustomer = applicationCustomer as RetailCustomer;
            retailCustomer!.NationalIdentity = customerDto.NationalIdentity;
            retailCustomer.Name = customerDto.Name;
            retailCustomer.CustomerType = customerDto.CustomerType;
            retailCustomer.Surname = customerDto.Surname;
            retailCustomer.PhoneNumber = customerDto.PhoneNumber;
        }
        else
        {
            if (applicationCustomer is RetailCustomer retailCust)
                retailCust.NationalIdentity = null;
            var corporateCustomer = applicationCustomer as CorporateCustomer;
            corporateCustomer!.TaxNumber = customerDto.TaxNumber;
            corporateCustomer.CorporateName = customerDto.CorporateName;
            corporateCustomer.Name = customerDto.Name;
            corporateCustomer.CustomerType = customerDto.CustomerType;
            corporateCustomer.Surname = customerDto.Surname;
            corporateCustomer.PhoneNumber = customerDto.PhoneNumber;
        }
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog(
            "Müşteri düzenleme isteği başarılı oldu.", LogType.Customer, LogAction.Update,
            EntityTypeCustomer, applicationCustomer.Id.ToString());
        return new SuccessDataResult<Customer>(applicationCustomer!);
    }

    public async Task<IResult> AddCustomer(CustomerAddDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Müşteri ekleme isteği geldi.", LogType.Customer, LogAction.Add, dto);
        await fluentValidator.ValidateAndThrowAsync(dto);
        Customer customer = dto.CustomerType == "Retail"
            ? mapper.MapToRetail(dto)
            : mapper.MapToCorporate(dto);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog(
            "Müşteri ekleme isteği başarılı oldu.", LogType.Customer, LogAction.Add,
            EntityTypeCustomer, customer.Id.ToString());
        var result = FuncMappings.CustomerToDetailDto()(customer);
        return new SuccessDataResult<CustomerDetailDto>(result, "Müşteri başarıyla eklendi.");
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo, int pageIndex = 0, int itemCount = 50)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.Customers.AsQueryable();
        if (!string.IsNullOrEmpty(customerInfo))
            query = query.Where(x =>
                (x as RetailCustomer)!.RetailSearchVector.Matches(EF.Functions.ToTsQuery(customerInfo.ToFullTextSearchQuery()))
                || (x as CorporateCustomer)!.CorporateSearchVector.Matches(EF.Functions.ToTsQuery(customerInfo.ToFullTextSearchQuery()))
                || x.FullName!.Contains(customerInfo));

        int total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.FullName)
            .Skip(pageIndex * itemCount)
            .Take(itemCount)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .ToListAsync();

        return new SuccessDataResult<Pageable<CustomerDetailDto>>(new Pageable<CustomerDetailDto>(items, pageIndex, itemCount, total));
    }

    public async Task<IResult> CheckIfCustomerExits(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var result = await dbContext.Customers.AnyAsync(x => x.Id == id);
        return result ? new SuccessResult() : new ErrorResult("Müşteri bulunamamıştır.");
    }

    public async Task<IDataResult<IEnumerable<CustomerDetailDto>>> GetCustomerBySearch(string searchText)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(searchText))
            throw new ValidationException("Arama kriteri boş olamaz.");

        var result = await dbContext.Customers
            .Where(x =>
                (x as RetailCustomer)!.RetailSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery()))
                || (x as CorporateCustomer)!.CorporateSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery())))
            .OrderBy(x => x.FullName)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .ToListAsync();
        return new SuccessDataResult<IEnumerable<CustomerDetailDto>>(result);
    }

    public async Task<IDataResult<Customer>> GetCustomerById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return new SuccessDataResult<Customer>((await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id))!);
    }

    public async Task<IDataResult<CustomerDetailDto>> GetCustomerDetailById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var cust = await dbContext.Customers
            .Where(c => c.Id == id)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .FirstOrDefaultAsync();
        return new SuccessDataResult<CustomerDetailDto>(cust!);
    }

    public async Task<IResult> SoftDelete(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var cust = await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (cust == null)
            return new ErrorResult(Messages.CustomerNotFound);
        cust.IsDeleted = true;
        cust.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog(
            "Müşteri silindi.", LogType.Customer, LogAction.Delete,
            EntityTypeCustomer, id.ToString());
        return new SuccessResult(Messages.ProcessSuccess);
    }

    public async Task<IResult> SetActive(int id, bool active, string? reason = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var cust = await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (cust == null)
            return new ErrorResult(Messages.CustomerNotFound);

        if (cust.IsActive == active)
            return new SuccessResult(Messages.ProcessSuccess);

        cust.IsActive = active;
        if (active)
        {
            cust.DeactivatedAt = null;
            cust.DeactivationReason = null;
        }
        else
        {
            cust.DeactivatedAt = DateTimeOffset.UtcNow;
            cust.DeactivationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        await dbContext.SaveChangesAsync();

        var content = active
            ? "Müşteri aktif hale getirildi."
            : $"Müşteri deaktif edildi.{(string.IsNullOrWhiteSpace(reason) ? "" : " Sebep: " + reason.Trim())}";
        await applicationLogManager.AddLog(
            content, LogType.Customer, LogAction.Update,
            EntityTypeCustomer, id.ToString());

        return new SuccessResult(content);
    }

    public async Task<IDataResult<List<CustomerActivityDto>>> GetCustomerActivity(int id, int maxItems = 200)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
            return new ErrorDataResult<List<CustomerActivityDto>>([], Messages.CustomerNotFound);

        var activities = new List<CustomerActivityDto>();
        var tr = CultureInfo.GetCultureInfo("tr-TR");

        activities.Add(new CustomerActivityDto
        {
            OccurredAt = customer.CreatedAt,
            Source = CustomerActivitySource.Lifecycle,
            Title = "Müşteri kaydı oluşturuldu",
            Icon = "user-plus",
            BadgeColor = "green"
        });

        if (customer.IsDeleted)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = customer.DeletedAt,
                Source = CustomerActivitySource.Lifecycle,
                Title = "Müşteri silindi",
                Icon = "trash",
                BadgeColor = "red"
            });
        }

        if (!customer.IsActive && customer.DeactivatedAt.HasValue)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = customer.DeactivatedAt.Value,
                Source = CustomerActivitySource.Lifecycle,
                Title = "Müşteri deaktif edildi",
                Description = customer.DeactivationReason,
                Icon = "user-off",
                BadgeColor = "orange"
            });
        }

        var idStr = id.ToString(CultureInfo.InvariantCulture);
        var logs = await dbContext.Logs
            .AsNoTracking()
            .Where(l => l.EntityType == EntityTypeCustomer && l.EntityId == idStr)
            .OrderByDescending(l => l.Id)
            .Take(maxItems)
            .Select(l => new
            {
                l.CreatedAt,
                l.Content,
                l.LogAction,
                UserName = l.ApplicationUser != null
                    ? (l.ApplicationUser.FullName ?? l.ApplicationUser.UserName)
                    : null
            })
            .ToListAsync();

        foreach (var l in logs)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = l.CreatedAt,
                Source = CustomerActivitySource.ApplicationLog,
                Title = l.Content ?? "İşlem",
                Icon = LogActionIcon(l.LogAction),
                BadgeColor = LogActionColor(l.LogAction),
                UserDisplayName = l.UserName
            });
        }

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(maxItems)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CreatedAt,
                o.OrderDate,
                o.MarketplaceOrderStatus,
                o.StorefrontOrderStatus,
                o.GrossAmount
            })
            .ToListAsync();

        foreach (var o in orders)
        {
            var status = o.MarketplaceOrderStatus
                         ?? (o.StorefrontOrderStatus.HasValue ? o.StorefrontOrderStatus.Value.ToString() : null);
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = o.OrderDate ?? o.CreatedAt,
                Source = CustomerActivitySource.Order,
                Title = string.IsNullOrEmpty(o.OrderNumber) ? "Sipariş oluşturuldu" : $"Sipariş #{o.OrderNumber}",
                Description = status,
                Icon = "shopping-cart",
                BadgeColor = "blue",
                RelatedUrl = $"/orders/{o.Id}",
                Amount = o.GrossAmount.HasValue ? o.GrossAmount.Value.ToString("N2", tr) + " ₺" : null
            });
        }

        var sales = await dbContext.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == id)
            .OrderByDescending(s => s.SaleDate)
            .Take(maxItems)
            .Select(s => new
            {
                s.Id,
                s.SaleNumber,
                s.SaleDate,
                s.SaleStatus,
                TotalAmount = s.SaleItems
                    .Select(si => (decimal?)(si.UnitPrice * si.Quantity))
                    .Sum() ?? 0m
            })
            .ToListAsync();

        foreach (var s in sales)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = s.SaleDate,
                Source = CustomerActivitySource.Sale,
                Title = string.IsNullOrEmpty(s.SaleNumber) ? "Satış yapıldı" : $"Satış {s.SaleNumber}",
                Description = s.SaleStatus.ToString(),
                Icon = "cash",
                BadgeColor = s.SaleStatus.ToString() == "Cancelled" ? "red" : "teal",
                RelatedUrl = $"/sales/{s.Id}",
                Amount = s.TotalAmount.ToString("N2", tr) + " ₺"
            });
        }

        var returns = await dbContext.SaleReturns
            .AsNoTracking()
            .Where(r => (r.Sale != null && r.Sale.CustomerId == id)
                     || (r.Order != null && r.Order.CustomerId == id))
            .OrderByDescending(r => r.ReturnDate)
            .Take(maxItems)
            .Select(r => new
            {
                r.Id,
                r.ReturnDate,
                r.ReturnStatus,
                r.RefundAmount
            })
            .ToListAsync();

        foreach (var r in returns)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = r.ReturnDate,
                Source = CustomerActivitySource.SaleReturn,
                Title = "İade talebi",
                Description = r.ReturnStatus.ToString(),
                Icon = "arrow-back-up",
                BadgeColor = "yellow",
                Amount = r.RefundAmount > 0 ? r.RefundAmount.ToString("N2", tr) + " ₺" : null
            });
        }

        var invoices = await dbContext.EFaturaRecords
            .AsNoTracking()
            .Where(e => e.Order != null && e.Order.CustomerId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(maxItems)
            .Select(e => new
            {
                e.CreatedAt,
                e.InvoiceId,
                e.InvoiceType,
                e.Status,
                e.PayableAmountKurus
            })
            .ToListAsync();

        foreach (var e in invoices)
        {
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = e.CreatedAt,
                Source = CustomerActivitySource.EInvoice,
                Title = (e.InvoiceType.ToString() == "EInvoice" ? "e-Fatura" : "e-Arşiv") +
                        (string.IsNullOrEmpty(e.InvoiceId) ? "" : $" #{e.InvoiceId}"),
                Description = e.Status.ToString(),
                Icon = "receipt",
                BadgeColor = "indigo",
                Amount = (e.PayableAmountKurus / 100m).ToString("N2", tr) + " ₺"
            });
        }

        var vouchers = await dbContext.DiscountVouchers
            .AsNoTracking()
            .Where(v => v.CustomerId == id)
            .OrderByDescending(v => v.CreatedAt)
            .Take(maxItems)
            .Select(v => new
            {
                v.CreatedAt,
                v.Code,
                v.DiscountType,
                v.Amount,
                v.Percentage,
                v.IsActive,
                v.CurrentUsageCount
            })
            .ToListAsync();

        foreach (var v in vouchers)
        {
            var desc = v.DiscountType.ToString() == "Percentage"
                ? $"%{v.Percentage} indirim"
                : v.Amount.ToString("N2", tr) + " ₺ indirim";
            activities.Add(new CustomerActivityDto
            {
                OccurredAt = v.CreatedAt,
                Source = CustomerActivitySource.DiscountVoucher,
                Title = string.IsNullOrEmpty(v.Code) ? "Kupon tanımlandı" : $"Kupon: {v.Code}",
                Description = desc + (v.CurrentUsageCount > 0 ? $" — {v.CurrentUsageCount} kez kullanıldı" : ""),
                Icon = "discount",
                BadgeColor = v.IsActive ? "pink" : "secondary"
            });
        }

        var sorted = activities
            .OrderByDescending(a => a.OccurredAt)
            .Take(maxItems)
            .ToList();

        return new SuccessDataResult<List<CustomerActivityDto>>(sorted);
    }

    private static string LogActionIcon(LogAction action) => action switch
    {
        LogAction.Add => "plus",
        LogAction.Update => "edit",
        LogAction.Delete => "trash",
        _ => "activity"
    };

    private static string LogActionColor(LogAction action) => action switch
    {
        LogAction.Add => "green",
        LogAction.Update => "blue",
        LogAction.Delete => "red",
        _ => "secondary"
    };
}
