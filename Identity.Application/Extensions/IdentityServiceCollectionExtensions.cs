using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.Extensions
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IdentityBuilder AddDefaultIdentity<TUser, TRole, TDbContext>(
            this IServiceCollection services)
            where TUser : IdentityUser
            where TRole : IdentityRole
            where TDbContext : DbContext
        {
            return services.AddIdentity<TUser, TRole>()
                .AddEntityFrameworkStores<TDbContext>()
                .AddDefaultTokenProviders();
        }
    }
}
