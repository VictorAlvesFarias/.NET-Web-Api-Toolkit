using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Helpers.Application.Extensions;
using Web.Api.Toolkit.Identity.Application.Configuration;

namespace Web.Api.Toolkit.Identity.Application.Extensions
{
    public static class UserManagerExtensions
    {
        public static List<Claim> GetDefaultClaims<T>(this UserManager<T> userManager, T user) where T : IdentityUser
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Nbf, now),
                new Claim(JwtRegisteredClaimNames.Iat, now)
            };

            return claims;
        }

        public static async Task<T> GetUser<T>(this UserManager<T> userManager, string accessKey) where T : IdentityUser
        {
            if (accessKey.IsEmail())
                return await userManager.FindByEmailAsync(accessKey);
            else
                return await userManager.FindByNameAsync(accessKey);
        }

        public static async Task<BaseResponse<string>> CreateDefaultToken<T>(this UserManager<T> userManager, T user, IOptions<JwtOptions> jwtOptions) where T : IdentityUser
        {
            var claims = userManager.GetDefaultClaims(user);

            var expiresDate = DateTime.Now.AddSeconds(jwtOptions.Value.AccessTokenExpiration);

            var jwt = new JwtSecurityToken(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claims,
                expires: expiresDate,
                notBefore: DateTime.Now,
                signingCredentials: jwtOptions.Value.SigningCredentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new BaseResponse<string>(true)
            {
                Data = token
            };

            return response;
        }
    }
}
