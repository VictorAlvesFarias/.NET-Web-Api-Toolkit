using Microsoft.AspNetCore.Identity;
using Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Identity.Application.Dtos;
using Web.Api.Toolkit.Identity.Domain.Entities;

namespace Web.Api.Toolkit.Identity.Application.Services
{
    public interface IIdentityUtilsService<T> where T : IdentityUser
    {
        Task<BaseResponse<LoginUserResponse>> LoginAsync(LoginUserRequest loginData);
        Task<DefaultResponse> DeleteSignedUser(LoginUserRequest email);
        Task<DefaultResponse> ValidateUsernameAsync(string email);
        Task<DefaultResponse> ValidateEmailAsync(string email);
        Task<DefaultResponse> ChangePasswordAsync(ChangePasswordRequest changePasswordData);
        Task<BaseResponse<string>> CreateToken(T user);
    }
}
