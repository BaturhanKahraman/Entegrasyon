using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Logic;
using Shared.Results;
namespace Entegrasyon.Business.Abstract
{
    public class RoleService(
        IApplicationLogManager applicationLogManager,
        IntegrationDbContext context,
        IMemoryCache cache,
        IValidator<AddRoleDto> addRoleDtoValidator,
        IValidator<EditRoleDto> editRoleDtoValidator) : IRoleService
    {
        private const string SelectListCache = "RoleSelectListCache";
        public async Task<IResult> AddRole(AddRoleDto dto, CancellationToken token = default)
        {
            var validationResult = await addRoleDtoValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return validationResult.ToResult();
            //TODO 
            var result = LogicRunner.Run(
                await CheckIfNameExists(dto.Name, token),
                await CheckIfClaimsExists(dto.Claims));
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
        private async Task<IResult> CheckIfClaimsExists(IEnumerable<int> claimsId)
        {
            var hasAll= await context.Claims.AllAsync(c=>claimsId.Contains(c.Id));
            return hasAll ? new SuccessResult() : new ErrorResult(Messages.NotExistingClaim);
        }
        private async Task<IResult> CheckIfNameExists(string name, CancellationToken token)
        {
            //normalize
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
            await applicationLogManager.AddLog("Rol güncelleniyor.", LogType.Role, LogAction.Update, dto);
            var dbRole = await context.Roles.FindAsync([dto.Id], token);
            if (dbRole is null)
                return new ErrorResult(Messages.RoleNotFound);

            var result = LogicRunner.Run(await CheckIfSameNameExists(dto.Name, dto.Id, token));
            if (result != null)
                return result;

            dbRole.Name = dto.Name;
            dbRole.NormalizedName = dto.Name.ToUpperInvariant();
            dbRole.Claims = dto.Claims.Select(c => new ApplicationClaim() { Id = c }).ToList();

            await context.SaveChangesAsync(token);

            await applicationLogManager.AddLog("Rol güncellendi.", LogType.Role, LogAction.Update);
            return new SuccessResult(Messages.RoleUpdated);
        }
        private async Task<IResult> CheckIfSameNameExists(string name, int id, CancellationToken token)
        {
            //normalize
            if (await context.Roles.AnyAsync(r => string.Equals(r.Name, name) && r.Id != id, token))
            {
                return new ErrorResult(Messages.RoleExits);
            }
            return new SuccessResult();
        }
        public async Task<IResult> DeleteRole(int id, CancellationToken token = default)
        {
            await applicationLogManager.AddLog("Rol siliniyor.", LogType.Role, LogAction.Delete, new { id });
            var dbRole = await context.Roles.FindAsync([id], token);
            if (dbRole is null)
                return new ErrorResult(Messages.RoleNotFound);
            context.Roles.Remove(dbRole);
            await context.SaveChangesAsync(token);
            await applicationLogManager.AddLog("Rol silindi", LogType.Role, LogAction.Delete);
            return new SuccessResult(Messages.RoleDeleted);
        }

        public async ValueTask<IEnumerable<Role>> GetRolesSelectList(CancellationToken token = default)
        {
            bool isCached = cache.TryGetValue(SelectListCache, out IEnumerable<Role> roles);
            if (isCached)
                return roles;
            roles = await context.Roles.Select(r => new Role() { Id = r.Id, Name = r.Name }).ToListAsync(token);
            cache.Set(SelectListCache, roles);
            return roles;
        }
    }
}
