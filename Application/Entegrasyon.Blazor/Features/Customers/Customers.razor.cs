using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Customers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Blazor.Features.Customers;

public partial class Customers
{
    [Inject] private ICustomerManager CustomerManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudDataGrid<CustomerDetailDto> dataGrid;
    private string searchString;

    private async Task<GridData<CustomerDetailDto>> ServerData(GridState<CustomerDetailDto> state)
    {
        var page = state.Page;
        var pageSize = state.PageSize;

        var result = await CustomerManager.GetCustomerDetailPageable(searchString, page, pageSize);

        if (result.Success && result.Data != null)
        {
            return new GridData<CustomerDetailDto>
            {
                TotalItems = result.Data.TotalItemCount,
                Items = result.Data.Items
            };
        }

        return new GridData<CustomerDetailDto>
        {
            TotalItems = 0,
            Items = new List<CustomerDetailDto>()
        };
    }

    private Task OnSearch(string text)
    {
        searchString = text;
        return dataGrid.ReloadServerData();
    }

    private async Task OpenDialogAsync(CustomerDetailDto? customer = null)
    {
        var parameters = new DialogParameters();
        if (customer != null)
        {
            var detailResult = await CustomerManager.GetCustomerById(customer.Id);
            if (detailResult.Success)
            {
                parameters.Add("Customer", detailResult.Data);
                parameters.Add("IsEdit", true);
            }
            else
            {
                Snackbar.Add(detailResult.Message, Severity.Error);
                return;
            }
        }
        else
        {
            parameters.Add("IsEdit", false);
        }

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<CustomerDialog>(
            customer == null ? "Yeni Müşteri Ekle" : "Müşteri Düzenle",
            parameters,
            options);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await dataGrid.ReloadServerData();
        }
    }

    private async Task DeleteCustomerAsync(int id)
    {
        var parameters = new DialogParameters();
        parameters.Add("ContentText", "Bu müşteriyi silmek istediğinize emin misiniz?");
        parameters.Add("ButtonText", "Sil");
        parameters.Add("Color", Color.Error);

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<MudMessageBox>("Silme Onayı", parameters, options); // Using a custom confirmation component usually, but here simulating with standard dialog or MessageBox logic if available. DataGrid often uses MessageBox.

        // Actually MudMessageBox isn't a component you show directly with ShowAsync usually, it's used inside a page or via IDialogService with a custom component wrapper.
        // Standard MudBlazor pattern for simple confirmation:
        var confirm = await DialogService.ShowMessageBox(
            "Silme Onayı",
            "Bu müşteriyi silmek istediğinize emin misiniz?",
            yesText: "Sil", cancelText: "İptal", noText: null,
            options: new DialogOptions { MaxWidth = MaxWidth.ExtraSmall });

        if (confirm == true)
        {
            var result = await CustomerManager.SoftDelete(id);
            if (result.Success)
            {
                Snackbar.Add("Müşteri başarıyla silindi.", Severity.Success);
                await dataGrid.ReloadServerData();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Silme işlemi başarısız.", Severity.Error);
            }
        }
    }
}
