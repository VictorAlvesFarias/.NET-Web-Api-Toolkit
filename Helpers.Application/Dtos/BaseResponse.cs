namespace Helpers.Application.Dtos
{
    public class BaseResponse<T> : DataResponse
    {
        public BaseResponse(bool success = false) => Success = success;
        public T? Data { get; set; }
    }
}
