using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Contracts;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public static async Task<PageResult<TResult>> CreateAsync<TSource, TResult>(
        IQueryable<TSource> query, int page, int pageSize, Func<TSource, TResult> map,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw BankingProblem.InvalidInput("Page must be positive and page size must be between 1 and 100.");

        var count = await query.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
            return new PageResult<TResult>([], page, pageSize, count);

        var rows = await query.Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new PageResult<TResult>(rows.Select(map).ToList(), page, pageSize, count);
    }
}
