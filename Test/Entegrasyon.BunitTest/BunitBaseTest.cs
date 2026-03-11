using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

namespace Entegrasyon.BunitTest;

/// <summary>
/// MudBlazor bileşenlerini test etmek için base sınıf.
/// JSInterop.Loose modu sayesinde MudBlazor'un JS çağrıları sessizce yok sayılır.
/// </summary>
public abstract class BunitBaseTest : TestContext
{
    protected Mock<IProductService> MockProductService { get; } = new();
    protected Mock<ICategoryService> MockCategoryService { get; } = new();
    protected Mock<IBrandService> MockBrandService { get; } = new();
    protected Mock<IBarcodeService> MockBarcodeService { get; } = new();
    protected Mock<IImageManager> MockImageManager { get; } = new();
    protected Mock<ISnackbar> MockSnackbar { get; } = new();

    protected BunitBaseTest()
    {
        // MudBlazor servislerini kaydet (IDialogService, IScrollManager vb.)
        Services.AddMudServices();

        // ISnackbar'ı mock ile değiştir (AddMudServices sonrası last-wins)
        Services.AddSingleton(MockSnackbar.Object);

        // Uygulama servisleri
        Services.AddSingleton(MockProductService.Object);
        Services.AddSingleton(MockCategoryService.Object);
        Services.AddSingleton(MockBrandService.Object);
        Services.AddSingleton(MockBarcodeService.Object);
        Services.AddSingleton(MockImageManager.Object);
        Services.AddSingleton(new EventChannel<ProductCreatedForMarketplaceEvent>());

        // MudBlazor'un JS çağrılarını (focus, scroll vb.) sessizce tolere et
        JSInterop.Mode = JSRuntimeMode.Loose;

        // MudPopoverProvider'ı PopoverService'e kayıt ettirmek için ayrıca render ediyoruz.
        // ChildContent parametresi olmadığı için RenderTree.TryAdd<T>() çalışmıyor;
        // tek yol bu şekilde standalone render etmek.
        RenderComponent<MudPopoverProvider>();
    }

    /// <summary>
    /// Reflection ile AddProduct bileşeninin private alanına değer atar
    /// ve bUnit'in renderer sync context'i üzerinde re-render tetikler.
    /// </summary>
    protected static async Task SetPrivateFieldAsync<TComponent>(
        IRenderedComponent<TComponent> cut,
        string fieldName,
        object? value)
        where TComponent : ComponentBase
    {
        await cut.InvokeAsync(() =>
        {
            var field = typeof(TComponent).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new InvalidOperationException($"'{fieldName}' private alanı bulunamadı");
            field.SetValue(cut.Instance, value);
        });
    }

    protected static T? GetPrivateField<TComponent, T>(
        IRenderedComponent<TComponent> cut,
        string fieldName)
        where TComponent : ComponentBase
    {
        var field = typeof(TComponent).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"'{fieldName}' private alanı bulunamadı");
        return (T?)field.GetValue(cut.Instance);
    }
}
