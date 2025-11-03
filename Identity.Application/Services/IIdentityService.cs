using Web.Api.Toolkit.Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Web.Api.Toolkit.Identity.Application.Dtos;

namespace Web.Api.Toolkit.Web.Api.Toolkit.Identity.Application.Services
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
