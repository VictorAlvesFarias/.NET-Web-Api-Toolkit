using Web.Api.Toolkit.Web.Api.Toolkit.Entity.Domain.Entities;

namespace Web.Api.Toolkit.Web.Api.Toolkit.Identity.Domain.Entities
{
    public class BaseUserOwnedEntity : BaseEntity
    {
        public string UserId { get; set; }
        public BaseEntityIdentity User { get; set; }
    }
}
