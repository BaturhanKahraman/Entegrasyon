using System.Security.Claims;
using Entegrasyon.Entity;
using Microsoft.Extensions.Options;
using Shared.Security.Jwt;
using Shared.User;

namespace Entegrasyon.Business.Utility.Auth;

public class ClaimHelper:JwtHelper
{
    public ClaimHelper(IOptions<TokenOptions> tokenOption) : base(tokenOption)
    {
    }

    protected override IEnumerable<Claim> SetClaims(RootUser user)
    {
        if (user is not ApplicationUser appUser)
            return base.SetClaims(user);
        Claims.AddRange(new []{new Claim("defaultbranchofficeid",appUser.DefaultBranchOfficeId.GetValueOrDefault().ToString()) });
        return base.SetClaims(user);
    }
}