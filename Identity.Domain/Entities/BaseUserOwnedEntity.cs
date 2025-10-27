using Domain.Entitites;
using Packages.Entity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Packages.Identity.Domain.Entities
{
    public class BaseUserOwnedEntity: BaseEntity
    {
        public string UserId { get; set; }
        public BaseEntityIdentity User { get; set; }
    }
}
