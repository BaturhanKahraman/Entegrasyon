using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontContactManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontContactManager
{
    public async Task<IResult> SubmitMessageAsync(int tenantId, string name, string email, string? phone, string? subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorResult("Ad soyad alanızorunludur.");

        if (string.IsNullOrWhiteSpace(email))
            return new ErrorResult("E-posta alanızorunludur.");

        if (string.IsNullOrWhiteSpace(message))
            return new ErrorResult("Mesaj alanızorunludur.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var contactMessage = new StorefrontContactMessage
        {
            TenantId = tenantId,
            Name = name,
            Email = email,
            Phone = phone,
            Subject = subject,
            Message = message,
            IsRead = false
        };

        dbContext.StorefrontContactMessages.Add(contactMessage);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Mesajiniz basariyla gonderildi. En kisa surede size donecegiz.");
    }

    public async Task<IDataResult<List<StorefrontContactMessage>>> GetMessagesAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var messages = await dbContext.StorefrontContactMessages
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontContactMessage>>(messages);
    }

    public async Task<IResult> MarkAsReadAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var msg = await dbContext.StorefrontContactMessages.FindAsync(id);
        if (msg is null)
            return new ErrorResult("Mesaj bulunamadı.");

        msg.IsRead = true;
        msg.ReadAt = DateTimeOffset.UtcNow;
        dbContext.StorefrontContactMessages.Update(msg);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Mesaj okundu olarak isaretlendi.");
    }
}
