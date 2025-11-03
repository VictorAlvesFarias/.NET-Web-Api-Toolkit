namespace Web.Api.Toolkit.Web.Api.Toolkit.Identity.Application.Dtos
{
    public class ChangePasswordRequest
    {
        public string IdentityUserId { get; set; }
        public string Passowrd { get; set; }
        public string ConfirmPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }

    }
}
