namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class BaseResponse<T> : DataResponse
    {
        public BaseResponse(bool success = true) => Success = success;
        public T? Data { get; set; }
    }
}
