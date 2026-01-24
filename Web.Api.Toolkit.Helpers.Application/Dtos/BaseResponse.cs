namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class BaseResponse
    {
        public BaseResponse() { }
        public BaseResponse(bool success) { Success = success; }

        public bool Success { get; set; }
        public List<ErrorMessage> Errors { get; set; } = new List<ErrorMessage>();

        public void AddError(ErrorMessage error)
        {
            Errors.Add(error);
        }

        public void AddErrors(List<ErrorMessage> errors)
        {
            Errors.AddRange(errors);
        }

        public void AddException(ErrorMessage error)
        {
            Errors.Add(error);
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public BaseResponse() { }
        public BaseResponse(bool success) : base(success) { }

        public T Data { get; set; }
    }
}