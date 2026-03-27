using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Results;
namespace Entegrasyon.Business.Concrete.Auth
{
    public class RoleService(
        IApplicationLogManager applicationLogManager,
        IDbContextFactory<IntegrationDbContext> contextFactory,
        TenantMemoryCache cache,
        IValidator<AddRoleDto> addRoleDtoValidator,
        IValidator<EditRoleDto> editRoleDtoValidator) : IRoleService
    {
        private const string SelectListCache = "RoleSelectListCache";
        public async Task<IResult> AddRole(AddRoleDto dto, CancellationToken token = default)
        {
            var validationResult = await addRoleDtoValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return validationResult.ToResult();
            await using var context = await contextFactory.CreateDbContextAsync();
            var result = LogicRunner.Run(
                await CheckIfNameExists(context, dto.Name, token));
            if (result != null)
                return result;
            await applicationLogManager.AddLog("Rol ekleniyor.", LogType.Role, LogAction.Add, dto);
            var role = new Role
            {
                Name = dto.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                NormalizedName = dto.Name.ToUpperInvariant()
            };
            await context.Roles.AddAsync(role, token);
            await context.SaveChangesAsync(token);
            await applicationLogManager.AddLog("Rol eklendi.", LogType.Role, LogAction.Add);
            return new SuccessResult(Messages.RoleAdded);
        }

        private static async Task<IResult> CheckIfNameExists(IntegrationDbContext context, string name, CancellationToken token)
        {
            if (await context.Roles.AnyAsync(r => string.Equals(r.Name, name), token))
            {
                return new ErrorResult(Messages.RoleExits);
            }
            return new SuccessResult();
        }

        public async Task<IResult> UpdateRole(EditRoleDto dto, CancellationToken token = default)
        {
            var validationResult = await editRoleDtoValidator.ValidateAsync(dto);
            if(!validationResult.IsValid)
                return validationResult.ToResult();
            await applicationLogManager.AddLog("Rol guncelleniyor.", LogType.Role, LogAction.Update, dto);
            await using var context = await contextFactory.CreateDbContextAsync();
            var dbRole = await context.Roles.FindAsync([dto.Id], token);
            if (dbRole is null)
                return new ErrorResult(Messages.RoleNotFound);

            var result = LogicRunner.Run(await CheckIfSameNameExists(context, dto.Name, dto.Id, token));
            if (result != null)
                return result;

            dbRole.Name = dto.Name;
            dbRole.NormalizedName = dto.Name.ToUpperInvariant();

            await context.SaveChangesAsync(token);

            await applicationLogManager.AddLog("Rol guncellendi.", LogType.Role, LogAction.Update);
            return new SuccessResult(Messages.RoleUpdated);
        }
        private static async Task<IResult> CheckIfSameNameExists(IntegrationDbContext context, string name, int id, CancellationToken token)
        {
            if (await context.Roles.AnyAsync(r => string.Equals(r.Name, name) && r.Id != id, token))
            {
                return new ErrorResult(Messages.RoleExits);
            }
            return new SuccessResult();
        }
        public async Task<IResult> DeleteRole(int id, CancellationToken token = default)
        {
            await applicationLogManager.AddLog("Rol siliniyor.", LogType.Role, LogAction.Delete, new { id });
            await using var context = await contextFactory.CreateDbContextAsync();
            var dbRole = await context.Roles.FindAsync([id], token);
            if (dbRole is null)
                return new ErrorResult(Messages.RoleNotFound);
            context.Roles.Remove(dbRole);
            await context.SaveChangesAsync(token);
            await applicationLogManager.AddLog("Rol silindi", LogType.Role, LogAction.Delete);
            return new SuccessResult(Messages.RoleDeleted);
        }

        public async Task<List<Role>> GetRolesWithClaimsAsync(CancellationToken token = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Roles
                .Include(r => r.RoleClaims)
                .ToListAsync(token);
        }

        public async ValueTask<IEnumerable<Role>> GetRolesSelectList(CancellationToken token = default)
        {
            bool isCached = cache.TryGetValue(SelectListCache, out IEnumerable<Role>? roles);
            if (isCached)
                return roles!;
            await using var context = await contextFactory.CreateDbContextAsync();
            roles = await context.Roles.Select(r => new Role() { Id = r.Id, Name = r.Name }).ToListAsync(token);
            cache.Set(SelectListCache, roles, TimeSpan.FromMinutes(30));
            return roles;
        }
    }
}
