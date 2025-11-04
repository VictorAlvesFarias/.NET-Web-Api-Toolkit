namespace Web.Api.Toolkit.Entity.Domain.Entities
{
    public class BaseEntity
    {
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Deleted { get; set; }
        public int Id { get; set; }
    }
}
