using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Common;

public static class IQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        string? sortColumn = null,
        string? sortDirection = "asc")
    {
        // Sorting nếu cần
        if (!string.IsNullOrWhiteSpace(sortColumn))
        {
            var prop = typeof(T).GetProperty(sortColumn);
            if (prop != null)
            {
                query = sortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(x => prop.GetValue(x, null))
                    : query.OrderBy(x => prop.GetValue(x, null));
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}