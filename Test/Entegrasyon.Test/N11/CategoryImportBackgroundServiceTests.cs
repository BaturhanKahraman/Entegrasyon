using Entegrasyon.Business.BackgroundServices;
using FluentAssertions;

namespace Entegrasyon.Test.N11;

public class CategoryImportBackgroundServiceTests
{
    [Fact]
    public void CategoryImportBackgroundService_ShouldExist()
    {
        typeof(CategoryImportBackgroundService)
            .Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }
}
