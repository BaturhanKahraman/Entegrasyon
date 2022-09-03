using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Shared.User;

namespace Shared.Security.Jwt
{
    public interface ITokenHelper
    {
        AccessToken CreateToken(RootUser user);

        JwtSecurityToken CreateJwtSecurityToken(TokenOptions tokenOptions,RootUser user,
            SigningCredentials signingCredentials,DateTime expires);
    }
}