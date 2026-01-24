namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class BaseResponse
    {
        public BaseResponse() { Success = true; }
        public BaseResponse(bool success) { Success = success; }

        public bool Success { get; set; }
        public List<ErrorMessage> Errors { get; set; } = new List<ErrorMessage>();

        public void AddError(ErrorMessage error)
        {
            Success = false;

            Errors.Add(error);
        }

        public void AddErrors(List<ErrorMessage> errors)
        {
            Success = false;

            Errors.AddRange(errors);
        }

        public void AddException(ErrorMessage error)
        {
            Success = false;

            Errors.Add(error);
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public BaseResponse() { Success = true; }
        public BaseResponse(bool success) : base(success) { }

        public T Data { get; set; }
    }
}