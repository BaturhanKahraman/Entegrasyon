using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// N11 pazaryerinden SOAP API üzerinden kategori ve kategori özelliklerini import eder.
/// </summary>
public class N11CategoryImporter : BaseCategoryImporterService
{
    private readonly IN11SoapClient _soapClient;
    private const string N11Namespace = "http://www.n11.com/ws/schemas";
    private const string WsdlPath = "CategoryService";
    private const string MarketplaceName = "N11";

    public override ImportSource Source => ImportSource.N11;

    public N11CategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IN11SoapClient soapClient,
        ILogger<N11CategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _soapClient = soapClient;
    }

    /// <summary>
    /// N11 SOAP API'sinden üst seviye kategorileri çeker.
    /// Her dönen kategori için HasChildren=true işaretlenir (alt kategoriler lazy load ile çekilir).
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new XElement(
                XName.Get("GetTopLevelCategoriesRequest", N11Namespace));

            var response = await _soapClient.SendAsync(WsdlPath, "", requestBody);

            var ns = XNamespace.Get(N11Namespace);
            var categoriesEl = response.Element(ns + "categories") ?? response.Element("categories");

            if (categoriesEl == null)
            {
                Logger.LogCritical("N11 üst seviye kategoriler boş döndü");
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, "N11 kategori API yanıtı boş döndü.");
            }

            var categories = categoriesEl
                .Elements(ns + "category")
                .Concat(categoriesEl.Elements("category"))
                .Select(el => new ExternalCategoryDto
                {
                    ExternalId = el.Element(ns + "id")?.Value ?? el.Element("id")?.Value ?? string.Empty,
                    Name = el.Element(ns + "name")?.Value ?? el.Element("name")?.Value ?? string.Empty,
                    HasChildren = true,
                    Children = new List<ExternalCategoryDto>()
                })
                .ToList();

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 üst seviye kategoriler çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Belirtilen kategorinin alt kategorilerini SOAP API üzerinden çeker.
    /// Yanıtta subCategoryList yoksa yaprak düğüm kabul edilir ve boş liste döner.
    /// </summary>
    public async Task<List<ExternalCategoryDto>> GetSubCategoriesAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new XElement(
                XName.Get("GetSubCategoriesRequest", N11Namespace),
                new XElement("categoryId", categoryId));

            var response = await _soapClient.SendAsync(WsdlPath, "", requestBody);

            var ns = XNamespace.Get(N11Namespace);
            var categoryEl = response.Element(ns + "category") ?? response.Element("category");

            if (categoryEl == null)
                return new List<ExternalCategoryDto>();

            var subCategoryListEl = categoryEl.Element(ns + "subCategoryList")
                                   ?? categoryEl.Element("subCategoryList");

            // Yaprak düğüm — alt kategori yok
            if (subCategoryListEl == null)
                return new List<ExternalCategoryDto>();

            var children = subCategoryListEl
                .Elements(ns + "subCategory")
                .Concat(subCategoryListEl.Elements("subCategory"))
                .Select(el => new ExternalCategoryDto
                {
                    ExternalId = el.Element(ns + "id")?.Value ?? el.Element("id")?.Value ?? string.Empty,
                    Name = el.Element(ns + "name")?.Value ?? el.Element("name")?.Value ?? string.Empty,
                    HasChildren = true, // Lazy detect: expand ile öğrenilir
                    Children = new List<ExternalCategoryDto>()
                })
                .ToList();

            return children;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 alt kategoriler çekilirken hata oluştu (categoryId={CategoryId})", categoryId);
            return new List<ExternalCategoryDto>();
        }
    }

    /// <summary>
    /// Import işleminden önce N11 marketplace kaydını yükler, ardından temel sınıfın
    /// transaction döngüsünü çalıştırır.
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri başarıyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata oluştu", Source);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    /// <summary>
    /// N11'e özgü kategori özellik (attribute) import işlemi.
    /// SOAP GetCategoryAttributes ile sayfalı olarak çeker; her özellik için
    /// marketplace-aware dedup uygular.
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;
        if (MarketPlace == null) return;

        try
        {
            var allAttributes = await FetchAllCategoryAttributesAsync(category.ExternalCategoryId, cancellationToken);

            if (!allAttributes.Any()) return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in allAttributes)
            {
                var dbCatAttr = await GetOrCreateAttributeAsync(dbContext, attr, cancellationToken);

                // Değerleri sadece yeni oluşturulan attribute'a ekle
                if (attr.IsNewlyCreated)
                {
                    foreach (var val in attr.Values)
                    {
                        await AddAttributeValueAsync(dbContext, val, dbCatAttr, cancellationToken);
                    }
                }

                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.Mandatory,
                    IsSlicer = attr.MultipleSelect,
                    IsVarianter = false
                });
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "N11 kategori özellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// GetCategoryAttributes SOAP çağrısını sayfalı yaparak tüm attribute'ları çeker.
    /// </summary>
    private async Task<List<N11AttributeDto>> FetchAllCategoryAttributesAsync(
        string externalCategoryId,
        CancellationToken cancellationToken)
    {
        var result = new List<N11AttributeDto>();
        int currentPage = 0;
        int pageSize = 100;
        int pageCount = 1;

        do
        {
            var requestBody = new XElement(
                XName.Get("GetCategoryAttributesRequest", N11Namespace),
                new XElement("categoryId", externalCategoryId),
                new XElement("pagingData",
                    new XElement("currentPage", currentPage),
                    new XElement("pageSize", pageSize)));

            var response = await _soapClient.SendAsync(WsdlPath, "", requestBody);

            var ns = XNamespace.Get(N11Namespace);
            var categoryEl = response.Element(ns + "category") ?? response.Element("category");

            if (categoryEl == null) break;

            // Sayfalama meta verisi
            var metaEl = categoryEl.Element(ns + "metadata") ?? categoryEl.Element("metadata");
            if (metaEl != null && currentPage == 0)
            {
                int.TryParse(metaEl.Element(ns + "pageCount")?.Value ?? metaEl.Element("pageCount")?.Value, out pageCount);
            }

            var attributeListEl = categoryEl.Element(ns + "attributeList") ?? categoryEl.Element("attributeList");
            if (attributeListEl == null) break;

            var attributes = attributeListEl
                .Elements(ns + "attribute")
                .Concat(attributeListEl.Elements("attribute"));

            foreach (var attrEl in attributes)
            {
                var id = ParseInt(attrEl.Element(ns + "id")?.Value ?? attrEl.Element("id")?.Value);
                var name = attrEl.Element(ns + "name")?.Value ?? attrEl.Element("name")?.Value ?? string.Empty;
                var mandatory = ParseBool(attrEl.Element(ns + "mandatory")?.Value ?? attrEl.Element("mandatory")?.Value);
                var multipleSelect = ParseBool(attrEl.Element(ns + "multipleSelect")?.Value ?? attrEl.Element("multipleSelect")?.Value);

                var valueListEl = attrEl.Element(ns + "valueList") ?? attrEl.Element("valueList");
                var values = new List<N11AttributeValueDto>();

                if (valueListEl != null)
                {
                    values = valueListEl
                        .Elements(ns + "value")
                        .Concat(valueListEl.Elements("value"))
                        .Select(vEl => new N11AttributeValueDto(
                            ParseInt(vEl.Element(ns + "id")?.Value ?? vEl.Element("id")?.Value),
                            vEl.Element(ns + "name")?.Value ?? vEl.Element("name")?.Value ?? string.Empty))
                        .ToList();
                }

                result.Add(new N11AttributeDto(id, name, mandatory, multipleSelect, values));
            }

            currentPage++;
        }
        while (currentPage < pageCount);

        return result;
    }

    /// <summary>
    /// Marketplace-aware dedup: N11 attribute ID'sine göre var olan attribute'u bulur
    /// ya da yeni oluşturur.
    /// </summary>
    private async Task<CategoryAttribute> GetOrCreateAttributeAsync(
        IntegrationDbContext dbContext,
        N11AttributeDto attr,
        CancellationToken cancellationToken)
    {
        // N11 marketplace match üzerinden ara
        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(
                m => m.MarketPlaceCategoryAttributeId == attr.Id
                  && m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existingMatch != null)
        {
            attr.IsNewlyCreated = false;
            return existingMatch.ApplicationCategoryAttribute;
        }

        // Yeni CategoryAttribute oluştur
        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.Name,
            CategoryAttributeHumanized = attr.Name,
            ImportId = attr.Id,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.CategoryAttributes.AddAsync(newAttr, cancellationToken);

        // Marketplace eşlemesi
        var match = new CategoryAttributeMarketPlaceMatch
        {
            MarketPlace = MarketPlace!,
            ApplicationCategoryAttribute = newAttr,
            MarketPlaceCategoryAttributeId = attr.Id
        };

        await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);

        attr.IsNewlyCreated = true;
        return newAttr;
    }

    /// <summary>
    /// CategoryAttributeValue ve eşleşen CategoryAttributeValueMarketPlaceMatch kaydını ekler.
    /// </summary>
    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        N11AttributeValueDto val,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = val.Name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace != null)
        {
            var valueMatch = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = val.Id
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(valueMatch, cancellationToken);
        }
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : 0;

    private static bool ParseBool(string? value) =>
        value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
        || value == "1";

    // -----------------------------------------------------------------------
    // İç DTO'lar
    // -----------------------------------------------------------------------

    private sealed class N11AttributeDto(int id, string name, bool mandatory, bool multipleSelect, List<N11AttributeValueDto> values)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public bool Mandatory { get; } = mandatory;
        public bool MultipleSelect { get; } = multipleSelect;
        public List<N11AttributeValueDto> Values { get; } = values;
        public bool IsNewlyCreated { get; set; }
    }

    private sealed record N11AttributeValueDto(int Id, string Name);
}
