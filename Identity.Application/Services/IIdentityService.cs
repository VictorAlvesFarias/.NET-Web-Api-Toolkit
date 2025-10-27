using Application.Dtos.User;
using Packages.Helpers.Application.Dtos;

namespace Packages.Identity.Application.Services
{
    public interface IIdentityService 
    {
        Task<BaseResponse<LoginUserResponse>> LoginAsync(LoginUserRequest loginData);
        Task<DefaultResponse> AddUser(CreateUserRequest userData);
        Task<DefaultResponse> DeleteSignedUser(LoginUserRequest email);
        Task<DefaultResponse> PutUser(PutUserRequest userData);
        Task<DefaultResponse> ValidateUsernameAsync(string email);
        Task<DefaultResponse> ValidateEmailAsync(string email);
        Task<DefaultResponse> ChangePasswordAsync(ChangePasswordRequest changePasswordData);
        Task<DefaultResponse> DeleteUser(string identityUserId);
    }
}
