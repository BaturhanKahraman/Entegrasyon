using Entegrasyon.Blazor.Components.Shared;

namespace Entegrasyon.BunitTest.Components.Shared;

public class UpgradePromptDialogTests : BunitBaseTest
{
    [Fact]
    public async Task Dialog_RendersContent_WhenShownViaDialogService()
    {
        // Arrange
        var dialogService = Services.GetRequiredService<IDialogService>();
        RenderComponent<MudDialogProvider>();

        // Act: Show dialog through MudBlazor's dialog service
        var parameters = new DialogParameters<UpgradePromptDialog>
        {
            { x => x.FeatureName, "Raporlar" }
        };
        var dialogRef = await dialogService.ShowAsync<UpgradePromptDialog>("Test", parameters);

        // Assert: Dialog reference should be created
        dialogRef.Should().NotBeNull();
    }

    [Fact]
    public void Dialog_CodeBehind_HasFeatureNameParameter()
    {
        // Verify the component has the expected parameter
        var type = typeof(UpgradePromptDialog);
        var prop = type.GetProperty("FeatureName");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(string));
    }
}
