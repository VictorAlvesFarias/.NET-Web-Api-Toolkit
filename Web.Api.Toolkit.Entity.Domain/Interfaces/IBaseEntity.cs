namespace Web.Api.Toolkit.Entity.Domain.Interfaces
{
    public interface IBaseEntity
    {
        DateTime CreateDate { get; set; }
        DateTime UpdateDate { get; set; }
        bool Deleted { get; set; }
        int Id { get; set; }
    }
}
