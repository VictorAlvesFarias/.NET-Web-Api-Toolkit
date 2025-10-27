using Entity.Domain.Entities;

namespace Identity.Domain.Entities
{
    public class BaseUserOwnedEntity : BaseEntity
    {
        public string UserId { get; set; }
        public BaseEntityIdentity User { get; set; }
    }
}
