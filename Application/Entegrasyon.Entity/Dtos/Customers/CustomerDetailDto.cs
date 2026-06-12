namespace Entegrasyon.Entity.Dtos.Customers;

public sealed class CustomerDetailDto
{
    public CustomerDetailDto()
    {

    }//
    public CustomerDetailDto(DateTimeOffset createdAt,int id,string nationalIdentityOrTaxNumber,string nameSurname,string corporateName,int salesCount,string phoneNumber,string fullAddress,string customerType,bool isActive = true,DateTimeOffset? deactivatedAt = null,string? deactivationReason = null)
    {
        CreatedAt = createdAt;
        Id = id;
        NationalIdentityOrTaxNumber = nationalIdentityOrTaxNumber;
        SalesCount = salesCount;
        PhoneNumber = phoneNumber;
        Address = fullAddress;
        CustomerType = customerType;
        NameSurname = nameSurname;
        CorporateName = corporateName;
        IsActive = isActive;
        DeactivatedAt = deactivatedAt;
        DeactivationReason = deactivationReason;
    }

    public string CustomerType { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public int Id { get; init; }
    public string NationalIdentityOrTaxNumber { get; init; } = null!;
    public string NameSurname { get; init; } = null!;
    public string CorporateName { get; init; } = null!;
    public int SalesCount { get; init; }
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset? DeactivatedAt { get; init; }
    public string? DeactivationReason { get; init; }

    // --- RFM segmentasyon (Müşteri Raporu) ---
    // Bu alanlar yalnızca CustomerReportManager.GetRfmPageableAsync tarafından doldurulur;
    // normal müşteri listesi (GetCustomerDetailPageable) bunları null bırakır → view "–" gösterir.
    /// <summary>Müşterinin son alışveriş (Order.OrderDate) tarihi — recency. Hiç siparişi yoksa null.</summary>
    public DateOnly? LastPurchaseDate { get; set; }

    /// <summary>Müşterinin toplam harcaması (Order.GrossAmount toplamı) — monetary. Hiç siparişi yoksa null.</summary>
    public decimal? TotalSpend { get; set; }

    /// <summary>RFM segment etiketi: "VIP" | "Risk" | "Yeni" | "Dormant" | "Standart". Hesaplanmadıysa null.</summary>
    public string? RfmSegment { get; set; }
}