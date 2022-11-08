using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Shared.Constants;
using Shared.Results;
using Shared.User;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete
{
    public class ApplicationRoleManager
    {
        private readonly IRoleManager<RootRole,RootClaim> _roleManager;
        private readonly ApplicationLogManager _applicationLogManager;
        private readonly FluentValidator _validator;
        public ApplicationRoleManager(IRoleManager<RootRole,RootClaim> roleManager,ApplicationLogManager applicationLogManager,FluentValidator validator)
        {
            _roleManager = roleManager;
            _applicationLogManager = applicationLogManager;
            _validator = validator;
        }

        public async Task<IResult> AddRole(AddRoleDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            await _applicationLogManager.AddLog("Rol ekleniyor.",LogType.Role,LogAction.Add,dto);
            var role =new RootRole { Name=dto.Name,
            CreatedAt=DateTimeOffset.UtcNow,
            };
            await _roleManager.AddRole(role,dto.Claims);
            await _applicationLogManager.AddLog("Rol eklendi.",LogType.Role,LogAction.Add);
            return new SuccessResult(Messages.RoleAdded);
        }

        public async Task<IResult> UpdateRole(EditRoleDto dto)
        {
            await _applicationLogManager.AddLog("Rol güncelleniyor.",LogType.Role,LogAction.Update,dto);
            await _roleManager.UpdateRole(dto.Id,dto.Name,dto.RootClaims);
            await _applicationLogManager.AddLog("Rol güncellendi.",LogType.Role,LogAction.Update);
            return new SuccessResult(Messages.RoleUpdated);
        }

        public async Task<IResult> DeleteRole(int id)
        {
            await _applicationLogManager.AddLog("Rol siliniyor.",LogType.Role,LogAction.Delete,new {id} );
            await _roleManager.DeleteRole(id);
            await _applicationLogManager.AddLog("Rol silindi",LogType.Role,LogAction.Delete);
            return new SuccessResult(Messages.RoleDeleted);
        }

    }
}
