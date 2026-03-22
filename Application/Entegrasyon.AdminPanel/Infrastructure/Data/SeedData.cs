using Entegrasyon.AdminPanel.Infrastructure.Auth;

namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(AdminPanelDbContext context)
    {
        if (context.AdminUsers.Any()) return;

        context.AdminUsers.Add(new AdminUser
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            DisplayName = "Sistem Yöneticisi",
            IsActive = true
        });

        context.SaveChanges();
    }
}
