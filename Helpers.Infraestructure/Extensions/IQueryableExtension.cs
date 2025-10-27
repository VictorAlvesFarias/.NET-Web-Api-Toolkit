using Helpers.Application.Dtos;

namespace Helpers.Infraestructure.Extensions
{
    public static class IQueryableExtension
    {
        public static IQueryable<T> Pagination<T>(this IQueryable<T> query, BaseGetRequest baseParams)
        {
            if (baseParams == null || baseParams.Page < 1 || baseParams.PageSize < 1)
            {
                return query;
            }

            return query.Skip((baseParams.Page - 1) * baseParams.PageSize).Take(baseParams.PageSize);
        }
    }
}
