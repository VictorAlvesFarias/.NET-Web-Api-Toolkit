using Microsoft.EntityFrameworkCore.Storage;

namespace Helpers.Infraestructure.Extensions
{
    public static class IDbContextTransactionExtension
    {
        public static void CommitDispose(this IDbContextTransaction context)
        {
            context.Commit();
            context.Dispose();
        }
        public static void RollbackDispose(this IDbContextTransaction context)
        {
            context.Rollback();
            context.Dispose();
        }
    }
}
