using Microsoft.AspNetCore.Identity;

namespace Web.Api.Toolkit.Identity.Domain.Entities
{
    public class BaseEntityIdentity : IdentityUser
    {
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Deleted { get; set; }
        public string Name { get; set; }
    }
}