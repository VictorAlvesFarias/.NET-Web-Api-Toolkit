namespace Helpers.Application.Dtos
{
    public class BaseGetRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? SearchInput { get; set; }
    }
}
