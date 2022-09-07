using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.User;
using System.Linq;

namespace Shared.Security.Jwt
{
    public class JwtHelper : ITokenHelper
    {
        private readonly IOptions<TokenOptions> _tokenOption;
        public JwtHelper(IOptions<TokenOptions> tokenOption)
        {
            _tokenOption = tokenOption;
        }

        public AccessToken CreateToken(RootUser user)
        {
            var expiration = DateTimeOffset.UtcNow.AddDays(_tokenOption.Value.AccessTokenExpiration);
            var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOption.Value.SecurityKey);
            var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);
            var jwt = CreateJwtSecurityToken(_tokenOption.Value,user,signingCredentials,expiration.DateTime);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtSecurityTokenHandler.WriteToken(jwt);
            return new AccessToken()
            {
                Token = token,ExpiresAt = expiration.DateTime
            };
        }
        public JwtSecurityToken CreateJwtSecurityToken(TokenOptions tokenOptions,RootUser user,
            SigningCredentials signingCredentials,DateTime expires)
        {
            var jwt = new JwtSecurityToken(
                issuer: tokenOptions.Issuer,
                audience: tokenOptions.Audience,
                expires: expires,
                notBefore: DateTime.Now,
                claims: SetClaims(user),
                signingCredentials: signingCredentials
            );
            return jwt;
        }
        private static IEnumerable<Claim> SetClaims(RootUser user)
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Email, user.Email),
                new (ClaimTypes.Name,user.Name),
                new (ClaimTypes.Surname,user.Surname),
                new (ClaimTypes.GivenName,user.UserName)
            };

            var userClaims =user.Roles.SelectMany(x => x.Claims)
                .Select(x => new Claim(ClaimTypes.Role, x.Name)).ToList();
            if(userClaims.Any())
                claims.AddRange(userClaims);
            return claims;
        }
    }
}
