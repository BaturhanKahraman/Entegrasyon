using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity;
using Shared.Results;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Pages.Customers;

public partial class CustomerDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
    [Inject] ICustomerManager CustomerManager { get; set; }
    [Inject] ISnackbar Snackbar { get; set; }

    [Parameter] public Customer? Customer { get; set; }
    [Parameter] public bool IsEdit { get; set; }

    private CustomerViewModel Model { get; set; } = new();
    private MudForm _form;
    private bool _success;
    private string[] _errors = { };

    protected override void OnInitialized()
    {
        if (IsEdit && Customer != null)
        {
            Model = new CustomerViewModel
            {
                Id = Customer.Id,
                CustomerType = Customer.CustomerType,
                PhoneNumber = Customer.PhoneNumber,
                FullAddress = Customer.Address?.FullAddress,
                Name = Customer.Name,
                Surname = Customer.Surname
            };

            if (Customer is RetailCustomer retail)
            {
                Model.NationalIdentity = retail.NationalIdentity;
                Model.CustomerType = "Retail";
            }
            else if (Customer is CorporateCustomer corporate)
            {
                Model.TaxNumber = corporate.TaxNumber;
                Model.CorporateName = corporate.CorporateName;
                Model.CustomerType = "Corporate";
            }
        }
        else
        {
            Model.CustomerType = "Retail"; // Default is Retail
        }
    }

    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string CustomerType { get; set; } = "Retail"; // "Retail" | "Corporate"
        public string Name { get; set; }
        public string Surname { get; set; }
        public string NationalIdentity { get; set; }
        public string CorporateName { get; set; }
        public string TaxNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string FullAddress { get; set; }
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        _form.Validate();
        if (!_success) return;

        if (IsEdit)
        {
            var dto = new UpdateCustomerDto(
                Model.Id,
                Model.PhoneNumber,
                Model.CustomerType,
                Model.Name,
                Model.Surname,
                Model.NationalIdentity,
                Model.TaxNumber,
                Model.CorporateName
            );

            var result = await CustomerManager.UpdateCustomer(dto);
            if (result.Success)
            {
                Snackbar.Add("Müşteri güncellendi.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }
        else
        {
            var dto = new CustomerAddDto(
                Model.NationalIdentity,
                Model.TaxNumber,
                Model.Name,
                Model.Surname,
                Model.CorporateName,
                Model.PhoneNumber,
                Model.FullAddress,
                Model.CustomerType
            );

            var result = await CustomerManager.AddCustomer(dto);
            if (result.Success)
            {
                Snackbar.Add("Müşteri eklendi.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }
    }
}
