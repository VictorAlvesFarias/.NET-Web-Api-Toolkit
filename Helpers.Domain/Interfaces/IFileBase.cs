namespace Helpers.Domain.Interfaces
{
    public interface IFileBase
    {
        byte[] GetBytes();
        string GetMimeType();
    }
}
