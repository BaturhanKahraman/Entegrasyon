using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class SellerProductController(
    IStorefrontTenantContext tenant,
    ISellerManager sellerManager,
    IDbContextFactory<IntegrationDbContext> contextFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register", "Seller");

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var products = await dbContext.SellerProducts
            .Include(x => x.Product)
            .Where(x => x.SellerId == seller.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.Seller = seller;
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Add([FromQuery] string? search)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register", "Seller");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existingProductIds = await dbContext.SellerProducts
            .Where(x => x.SellerId == seller.Id)
            .Select(x => x.ProductId)
            .ToListAsync();

        var query = dbContext.MainProducts
            .Where(x => !existingProductIds.Contains(x.Id));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Title.Contains(search));

        var products = await query
            .OrderBy(x => x.Title)
            .Take(50)
            .ToListAsync();

        ViewBag.Seller = seller;
        ViewBag.Search = search;
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, decimal price, int stock)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register", "Seller");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var already = await dbContext.SellerProducts
            .AnyAsync(x => x.SellerId == seller.Id && x.ProductId == productId);

        if (already)
        {
            ViewBag.Error = "Bu urun zaten listenizde.";
            return RedirectToAction("Index");
        }

        var sellerProduct = new SellerProduct
        {
            SellerId = seller.Id,
            ProductId = productId,
            Price = price,
            Stock = stock,
            IsActive = true,
            Status = SellerProductStatus.Pending
        };

        dbContext.SellerProducts.Add(sellerProduct);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, decimal price, int stock, bool isActive)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register", "Seller");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sellerProduct = await dbContext.SellerProducts
            .FirstOrDefaultAsync(x => x.Id == id && x.SellerId == seller.Id);

        if (sellerProduct is null)
            return NotFound();

        sellerProduct.Price = price;
        sellerProduct.Stock = stock;
        sellerProduct.IsActive = isActive;

        dbContext.SellerProducts.Update(sellerProduct);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    private async Task<Seller?> GetApprovedSellerAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var customerId)) return null;

        var result = await sellerManager.GetSellerByCustomerIdAsync(tenant.TenantId, customerId);
        if (!result.Success || result.Data.Status != SellerStatus.Approved) return null;

        return result.Data;
    }
}
