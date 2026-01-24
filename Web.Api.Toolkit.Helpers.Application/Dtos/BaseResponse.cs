namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public List<ErrorMessage> Errors { get; set; } = new List<ErrorMessage>();

        public void AddError(ErrorMessage error)
        {
            Errors.Add(error);
        }

        public void AddException(ErrorMessage error)
        {
            // Sua lógica de exceção aqui
            Errors.Add(error);
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public T Data { get; set; }
    }
}