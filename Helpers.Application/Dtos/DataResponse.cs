namespace Helpers.Application.Dtos
{
    public class DataResponse
    {
        public List<ErrorMessage> Errors { get; set; } = new List<ErrorMessage>();
        public List<ErrorMessage> Exceptions { get; set; } = new List<ErrorMessage>();
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
        public bool Success { get; set; }
    }
}
