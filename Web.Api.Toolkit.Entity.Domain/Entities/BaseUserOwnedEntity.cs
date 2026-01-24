using Web.Api.Toolkit.Helpers.Domain.Interfaces;

namespace Web.Api.Toolkit.Entity.Domain.Entities
{
    public class BaseUserOwnedEntity<TUser> : BaseEntity, IBaseUserOwnedEntity<TUser>
    {
        public TUser User { get; set ; }
        public string UserId { get; set; }
    }
}
