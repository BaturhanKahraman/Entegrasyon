using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.User;

namespace Shared.Security.Jwt
{
    public class JwtHelper : ITokenHelper
    {
        protected List<Claim> Claims = new ();
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
        protected virtual IEnumerable<Claim> SetClaims(RootUser user)
        {
            Claims.AddRange(new List<Claim>
            {
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Name,user.Name),
                new (ClaimTypes.Surname,user.Surname),
                new (ClaimTypes.GivenName,user.UserName),
                new (ClaimTypes.Role,user.Role.Name),
            });
            var userClaims =user.Role.Claims.Select(x => new Claim("authorizationgroup", x.Name)).ToList();
            if(userClaims.Any())
                Claims.AddRange(userClaims);
            return Claims;
        }
    }
}
