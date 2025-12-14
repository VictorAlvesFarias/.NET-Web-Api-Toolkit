using Web.Api.Toolkit.Helpers.Domain.Interfaces;

namespace Web.Api.Toolkit.Entity.Domain.Entities
{
    public class BaseEntity : IBaseEntity
    {
        public BaseEntity() {
            CreateDate = DateTime.UtcNow;
            UpdateDate = DateTime.UtcNow;
        }

        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Deleted { get; set; }
        public int Id { get; set; }
    }
}
