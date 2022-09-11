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
        public ApplicationRoleManager(IRoleManager<RootRole,RootClaim> roleManager,ApplicationLogManager applicationLogManager)
        {
            _roleManager = roleManager;
            _applicationLogManager = applicationLogManager;
        }

        public async Task<IResult> AddRole(AddRoleDto dto)
        {
            var addingLog = _applicationLogManager.AddLog("Rol ekleniyor.",LogType.Role,LogAction.Add,dto);
            var role = new RootRole { Name=dto.RoleName,Claims=dto.RootClaims};
            var addingRole = _roleManager.AddRole(role);
            await Task.WhenAll(addingLog,addingRole);
            await _applicationLogManager.AddLog("Rol eklendi.",LogType.Role,LogAction.Add);
            return new SuccessResult(Messages.RoleAdded);
        }

        public async Task<IResult> UpdateRole(RootRole role,List<RootClaim> claims)
        {
            var addingLog = _applicationLogManager.AddLog("Rol ekleniyor.",LogType.Role,LogAction.Add,new { role = role,claims = claims });
            var addingRole = _roleManager.UpdateRole(role,claims);
            await Task.WhenAll(addingLog,addingRole);
            await _applicationLogManager.AddLog("Rol eklendi.",LogType.Role,LogAction.Add);
            return new SuccessResult(Messages.RoleUpdated);
        }


    }
}
