using Entegrasyon.ApplicationBootstrap.Security;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Security;

/// <summary>
/// Kategori eşleme silme (unmap cascade) için ayrı yetkinin policy registration'a
/// (GetAllPermissions) dahil olduğunu doğrular — Program.cs bu listeden policy üretir.
/// </summary>
public class CategoryDeleteMappingPermissionTests
{
    [Fact]
    public void GetAllPermissions_includes_category_delete_mapping()
    {
        AppPermissions.GetAllPermissions()
            .Should().Contain(AppPermissions.Categories.DeleteMapping);
    }
}
