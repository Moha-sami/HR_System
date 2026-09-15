using System.Linq.Expressions;

namespace Buy2.Application.Common.Specifications;

/// <summary>
/// Persistence-agnostic sort instruction. Interpreted by the infrastructure
/// layer (EF Core today); application code never touches IQueryable.
/// </summary>
public sealed class Ordering<T>
{
    public Expression<Func<T, object>> KeySelector { get; }
    public bool Descending { get; }

    public Ordering(Expression<Func<T, object>> keySelector, bool descending = false)
    {
        KeySelector = keySelector;
        Descending = descending;
    }
}

/// <summary>
/// Persistence-agnostic query description: filter + eager-load paths +
/// ordering + paging + tracking behavior. Lives in Application so handlers
/// compose queries without referencing any ORM.
/// Include paths use dotted navigation names, e.g. "JobRole.Department".
/// </summary>
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<string> Includes { get; }
    IReadOnlyList<Ordering<T>> Orderings { get; }
    int? Skip { get; }
    int? Take { get; }
    bool Tracked { get; }
    bool IgnoreQueryFilters { get; }
}

public class Specification<T> : ISpecification<T> where T : class
{
    private readonly List<Expression<Func<T, bool>>> _wheres = new();
    private readonly List<string> _includes = new();
    private readonly List<Ordering<T>> _orderings = new();

    public Expression<Func<T, bool>>? Criteria =>
        _wheres.Count == 0 ? null : _wheres.Aggregate(ExpressionCombiner.AndAlso);

    public IReadOnlyList<string> Includes => _includes;
    public IReadOnlyList<Ordering<T>> Orderings => _orderings;
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool Tracked { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    public Specification<T> Where(Expression<Func<T, bool>> predicate)
    {
        _wheres.Add(predicate);
        return this;
    }

    public Specification<T> Include(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path) && !_includes.Contains(path))
            {
                _includes.Add(path);
            }
        }

        return this;
    }

    public Specification<T> OrderBy(Expression<Func<T, object>> keySelector, bool descending = false)
    {
        _orderings.Add(new Ordering<T>(keySelector, descending));
        return this;
    }

    public Specification<T> ThenBy(Expression<Func<T, object>> keySelector, bool descending = false)
    {
        return OrderBy(keySelector, descending);
    }

    public Specification<T> Page(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    public Specification<T> AsTracked()
    {
        Tracked = true;
        return this;
    }

    public Specification<T> IgnoreFilters()
    {
        IgnoreQueryFilters = true;
        return this;
    }
}

/// <summary>
/// Paged query result. TotalCount is computed from the filter
/// without paging; Items contains only the requested page.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

internal static class ExpressionCombiner
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var body = Expression.AndAlso(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
