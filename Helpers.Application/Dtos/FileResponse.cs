namespace Web.Api.Toolkit.Helpers.Application.Dtos
{
    public class FileResponse : BaseResponse<MemoryStream>
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
    }
}
