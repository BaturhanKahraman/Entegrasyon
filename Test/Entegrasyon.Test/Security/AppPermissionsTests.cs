using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Constants;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Security;

/// <summary>
/// Faz 1: Yeni onay claim'lerinin AppPermissions'a eklendiğini ve
/// Business katmanındaki PermissionConstants mirror'ın senkron olduğunu doğrular.
/// </summary>
public class AppPermissionsTests
{
    [Fact]
    public void GetAllPermissions_includes_new_approval_claims()
    {
        var all = AppPermissions.GetAllPermissions();

        all.Should().Contain(AppPermissions.StockOffice.DeleteApprove);
        all.Should().Contain(AppPermissions.Stock.Transfer);
        all.Should().Contain(AppPermissions.Stock.TransferApprove);
    }

    [Fact]
    public void StockOffice_DeleteApprove_has_expected_string_value()
    {
        AppPermissions.StockOffice.DeleteApprove
            .Should().Be("Permissions.StockOffice.Delete.Approve");
    }

    [Fact]
    public void Stock_Transfer_has_expected_string_values()
    {
        AppPermissions.Stock.Transfer.Should().Be("Permissions.Stock.Transfer");
        AppPermissions.Stock.TransferApprove.Should().Be("Permissions.Stock.Transfer.Approve");
    }

    [Fact]
    public void GetAllPermissions_contains_no_duplicates()
    {
        var all = AppPermissions.GetAllPermissions();
        all.Distinct().Should().HaveCount(all.Count,
            "her permission AppPermissions.GetAllPermissions() içinde tam olarak bir kez olmalı");
    }

    /// <summary>
    /// PermissionConstants (Business katmanı) ve AppPermissions (Bootstrap katmanı) string'leri
    /// birebir eşleşmeli. Mirror pattern dairesel referans olmadan tutarlılık sağlıyor.
    /// </summary>
    [Fact]
    public void PermissionConstants_mirror_AppPermissions_strings_exactly()
    {
        PermissionConstants.BranchOfficeDelete.Should().Be(AppPermissions.BranchOffices.Delete);
        PermissionConstants.StockOfficeDeleteApprove.Should().Be(AppPermissions.StockOffice.DeleteApprove);
        PermissionConstants.StockTransfer.Should().Be(AppPermissions.Stock.Transfer);
        PermissionConstants.StockTransferApprove.Should().Be(AppPermissions.Stock.TransferApprove);
    }
}
