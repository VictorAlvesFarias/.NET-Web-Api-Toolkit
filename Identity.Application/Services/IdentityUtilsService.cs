using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Helpers.Application.Extensions;
using Web.Api.Toolkit.Identity.Application.Configuration;
using Web.Api.Toolkit.Identity.Application.Dtos;
using Web.Api.Toolkit.Identity.Domain.Entities;

namespace Web.Api.Toolkit.Identity.Application.Services
{
    public class IdentityUtilsService<T> : IIdentityUtilsService<T> where T : IdentityUser
    {
        private readonly SignInManager<T> _singInManager;
        private readonly UserManager<T> _userManager;
        private readonly JwtOptions _jwtOptions;

        public IdentityUtilsService(
            SignInManager<T> signInManager,
            UserManager<T> userManager,
            IOptions<JwtOptions> jwtOptions
        )
        {
            _singInManager = signInManager;
            _userManager = userManager;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<DefaultResponse> DeleteSignedUser(LoginUserRequest loginData)
        {
            var user = await GetUserByEmailOrUsername(loginData.AccessKey);

            var login = await _singInManager.PasswordSignInAsync(user, loginData.Password, false, false);

            var response = new DefaultResponse(login.Succeeded);

            if (login.Succeeded)
            {
                _userManager.DeleteAsync(user);

                return response;
            }

            else
            {
                response.AddError(new ErrorMessage("Senha ou Usuario incorretos."));

                return response;
            }
        }

        public async Task<DefaultResponse> ValidateEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            var response = new DefaultResponse(user == null);

            if (response.Success)
            {
                return response;
            }

            else
            {
                response.AddError(new ErrorMessage("E-mail already used."));

                return response;
            }
        }

        public async Task<BaseResponse<LoginUserResponse>> LoginAsync(LoginUserRequest loginData)
        {
            var user = await GetUserByEmailOrUsername(loginData.AccessKey);
            var login = await _singInManager.PasswordSignInAsync(user, loginData.Password, false, false);
            var response = new BaseResponse<LoginUserResponse>(login.Succeeded);

            if (!login.Succeeded)
            {
                response.AddError(new ErrorMessage("Senha ou Usuario incorretos."));

                return response;
            }

            var token = await CreateToken(user);

            response.Data = new()
            {
                Email = user.Email,
                ExpectedExpirationTokenDateTime = DateTime.UtcNow.AddSeconds(_jwtOptions.AccessTokenExpiration),
                Username = user.UserName,
                Id = user.Id,
                ExpirationTokenTime = _jwtOptions.AccessTokenExpiration,
                Token = token.Data
            };

            return response;
        }

        public async Task<IList<Claim>> GetClaimsAndRoles(T user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));

            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTime.Now.ToString()));

            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()));

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            };

            return claims;
        }

        public async Task<BaseResponse<string>> CreateToken(T user)  
        {
            var claims = await GetClaimsAndRoles(user);
            var expiresDate = DateTime.Now.AddSeconds(_jwtOptions.AccessTokenExpiration);
            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresDate,
                notBefore: DateTime.Now,
                signingCredentials: _jwtOptions.SigningCredentials
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            var response = new BaseResponse<string>(true) { 
                Data = token
            };

            return response;
        }

        public async Task<T> GetUserByEmailOrUsername(string accessKey)
        {
            var user = accessKey.IsEmail() ?
                await _userManager.FindByEmailAsync(accessKey) :
                await _userManager.FindByNameAsync(accessKey);

            return user;
        }

        public async Task<DefaultResponse> ValidateUsernameAsync(string email)
        {
            var user = await _userManager.FindByNameAsync(email);
            var response = new DefaultResponse(user == null);

            if (response.Success)
            {
                return response;
            }
            else
            {
                response.AddError(new ErrorMessage("Nome de usuário já utilizado."));

                return response;
            }
        }

        public async Task<DefaultResponse> ChangePasswordAsync(ChangePasswordRequest changePasswordData)
        {
            var user = await _userManager.FindByIdAsync(changePasswordData.IdentityUserId);
            var changedPassword = await _userManager.ChangePasswordAsync(user, changePasswordData.Passowrd, changePasswordData.NewPassword);
            var response = new DefaultResponse(changedPassword.Succeeded);

            if (response.Success)
            {
                return response;
            }
            else
            {
                response.AddErrors(changedPassword.Errors.ToList().ConvertAll(item => new ErrorMessage(item.Description)));
            }

            throw new NotImplementedException();
        }
    }
}