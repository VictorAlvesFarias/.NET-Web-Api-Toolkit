namespace Web.Api.Toolkit.Helpers.Domain.Interfaces
{
    public interface IFileBase
    {
        byte[] Bytes { get; set; }
        string MimeType { get; set; }
    }
}
