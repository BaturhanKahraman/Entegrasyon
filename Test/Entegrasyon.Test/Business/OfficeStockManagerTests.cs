using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Business;

public class OfficeStockManagerTests : BaseTest
{
    private readonly IOfficeStockManager _manager;
    private readonly Mock<IBranchOfficeManager> _mockBranchOfficeManager = new();
    private readonly Mock<IProductVariantManager> _mockProductVariantManager = new();
    private readonly Mock<INotificationManager> _mockNotificationManager = new();
    private readonly Mock<ILogger<OfficeStockManager>> _mockLogger = new();

    public OfficeStockManagerTests()
    {
        _manager = new OfficeStockManager(
            mockContextFactory.Object,
            _mockBranchOfficeManager.Object,
            _mockProductVariantManager.Object,
            _mockNotificationManager.Object,
            new EventChannel<StockPriceChangedEvent>(),
            mockTenantContext.Object,
            _mockLogger.Object
        );
    }

    // Bug #3: CheckIfProductCountZero ("en az bir stok gir" iş kuralı) kaldırıldı —
    // esnaf artık stoksuz ürün ekleyebiliyor. İlgili testler silindi.
}
