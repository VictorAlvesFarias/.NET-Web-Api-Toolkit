namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public List<ErrorMessage> Errors { get; set; } = new List<ErrorMessage>();
        public List<ErrorMessage> Exceptions { get; set; } = new List<ErrorMessage>();

        public BaseResponse(bool success = true) => Success = success;

        public void AddError(ErrorMessage error)
        {
            Errors.Add(error);
            Success = false;
        }
        public void AddErrors(List<ErrorMessage> errors)
        {
            Errors.AddRange(errors);
            Success = false;
        }
        public void AddException(ErrorMessage error)
        {
            Exceptions.Add(error);
            Success = false;
        }
        public void AddExceptions(List<ErrorMessage> errors)
        {
            Exceptions.AddRange(errors);
            Success = false;
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public BaseResponse(bool success = true) : base(success) { }

        public T? Data { get; set; }
    }
}
