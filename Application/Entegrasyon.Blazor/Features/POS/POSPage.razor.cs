using System.Globalization;
using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.POS;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.POS;

public partial class POSPage : ComponentBase
{
    [Inject] private IPOSSessionManager POSSessionManager { get; set; } = null!;
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = null!;
    [Inject] private IProductVariantManager ProductVariantManager { get; set; } = null!;
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private static readonly CultureInfo _trCulture = new("tr-TR");

    // Session state
    private bool _hasActiveSession;
    private POSSession? _activeSession;
    private int _selectedBranchOfficeId = 1;
    private decimal _openingCash;
    private string? _terminalId;
    private string _currentUserName = "";
    private string _currentBranchName = "";
    private Guid _currentUserId;
    private int _sessionTransactionCount;
    private bool _processing;

    // Cart state
    private string _barcodeSearch = string.Empty;
    private readonly List<CartItem> _cartItems = [];
    private MudTextField<string>? _barcodeField;

    // Branch offices for selector
    private List<BranchOffice> _branchOffices = [];

    // Computed properties
    private decimal Subtotal => _cartItems.Sum(x => x.LineSubtotal);
    private decimal TotalDiscount => _cartItems.Sum(x => x.LineDiscount);
    private decimal TotalTax => _cartItems.Sum(x => x.LineTax);
    private decimal GrandTotal => _cartItems.Sum(x => x.LineTotal);

    protected override async Task OnInitializedAsync()
    {
        var branchResult = await BranchOfficeManager.GetBranchList();
        if (branchResult.Success)
            _branchOffices = branchResult.Data;

        // Get current user from auth state
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out _currentUserId))
        {
            var userResult = await UserManager.GetUserDetails(_currentUserId);
            if (userResult.Success && userResult.Data != null)
            {
                _currentUserName = $"{userResult.Data.Name} {userResult.Data.Surname}".Trim();
            }
        }

        // Check for active session
        await CheckActiveSession();
    }

    private async Task CheckActiveSession()
    {
        var result = await POSSessionManager.GetActiveSessionAsync(_selectedBranchOfficeId, _terminalId);
        if (result.Success)
        {
            _activeSession = result.Data;
            _hasActiveSession = true;
            _currentBranchName = _branchOffices.FirstOrDefault(b => b.Id == _activeSession.BranchOfficeId)?.Name ?? "";
        }
        else
        {
            _hasActiveSession = false;
            _activeSession = null;
        }
    }

    private async Task OpenSession()
    {
        _processing = true;
        try
        {
            var dto = new OpenSessionDto(_selectedBranchOfficeId, _currentUserId, _openingCash, _terminalId);
            var result = await POSSessionManager.OpenSessionAsync(dto);
            if (result.Success)
            {
                _activeSession = result.Data;
                _hasActiveSession = true;
                _currentBranchName = _branchOffices.FirstOrDefault(b => b.Id == _selectedBranchOfficeId)?.Name ?? "";
                _sessionTransactionCount = 0;
                Snackbar.Add("Kasa basariyla acildi!", Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message ?? "Kasa acilamadi.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _processing = false;
        }
    }

    private async Task HandleBarcodeSearch(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_barcodeSearch))
        {
            await SearchAndAddProduct(_barcodeSearch.Trim());
            _barcodeSearch = string.Empty;
        }
    }

    private async Task SearchAndAddProduct(string barcode)
    {
        try
        {
            var result = await ProductVariantManager.GetProductVariantByBarcode(barcode);
            if (result.Success && result.Data != null)
            {
                var variant = result.Data;
                var existing = _cartItems.FirstOrDefault(x => x.ProductVariantId == variant.ProductVariantId);
                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    _cartItems.Add(new CartItem
                    {
                        ProductVariantId = variant.ProductVariantId,
                        ProductName = variant.ProductName,
                        Barcode = barcode,
                        Quantity = 1,
                        UnitPrice = variant.SalePrice > 0 ? variant.SalePrice : variant.ListPrice,
                        DiscountPercent = 0,
                        VatRate = variant.TaxPercentage > 0 ? variant.TaxPercentage : 20
                    });
                }
                Snackbar.Add($"Eklendi: {variant.ProductName}", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Barkoda ait urun bulunamadi: {barcode}", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Urun arama hatasi: {ex.Message}", Severity.Error);
        }
    }

    private void RemoveFromCart(CartItem item)
    {
        _cartItems.Remove(item);
        Snackbar.Add("Urun sepetten cikarildi.", Severity.Info);
    }

    private void ClearCart()
    {
        _cartItems.Clear();
        Snackbar.Add("Sepet temizlendi.", Severity.Info);
    }

    private async Task OpenPaymentDialog()
    {
        var parameters = new DialogParameters<POSPaymentDialog>
        {
            { x => x.GrandTotal, GrandTotal },
            { x => x.CartItems, _cartItems },
            { x => x.ActiveSession, _activeSession! },
            { x => x.CurrentUserId, _currentUserId },
            { x => x.BranchOfficeId, _activeSession!.BranchOfficeId }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<POSPaymentDialog>("Odeme", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            _cartItems.Clear();
            _sessionTransactionCount++;
            Snackbar.Add("Satis basariyla tamamlandi!", Severity.Success);
        }
    }

    private async Task ShowSummary()
    {
        if (_activeSession == null) return;

        var parameters = new DialogParameters<POSSummaryDialog>
        {
            { x => x.SessionId, _activeSession.Id }
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        await DialogService.ShowAsync<POSSummaryDialog>("Oturum Ozeti", parameters, options);
    }

    private async Task CloseSessionDialog()
    {
        if (_activeSession == null) return;

        var parameters = new DialogParameters<POSCloseSessionDialog>
        {
            { x => x.SessionId, _activeSession.Id }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = false,
            BackdropClick = false
        };

        var dialog = await DialogService.ShowAsync<POSCloseSessionDialog>("Kasayi Kapat", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            _hasActiveSession = false;
            _activeSession = null;
            _cartItems.Clear();
            _sessionTransactionCount = 0;
            Snackbar.Add("Kasa basariyla kapatildi!", Severity.Success);
        }
    }

    public class CartItem
    {
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal VatRate { get; set; } = 20;

        public decimal LineSubtotal => Quantity * UnitPrice;
        public decimal LineDiscount => LineSubtotal * ((decimal)DiscountPercent / 100);
        public decimal LineAfterDiscount => LineSubtotal - LineDiscount;
        public decimal LineTax => LineAfterDiscount * (VatRate / 100);
        public decimal LineTotal => LineAfterDiscount + LineTax;
    }
}
