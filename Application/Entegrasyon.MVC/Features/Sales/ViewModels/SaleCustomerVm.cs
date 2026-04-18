namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public class SaleCustomerSearchVm
{
    public List<SaleCustomerItemVm> Customers { get; set; } = [];
}

public class SaleCustomerItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
