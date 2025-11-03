namespace Web.Api.Toolkit.Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class DefaultResponse : BaseResponse<DefaultResponse>
    {
        public DefaultResponse(bool success = true) => Success = success;
    }
}
