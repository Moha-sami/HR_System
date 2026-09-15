using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Buy2.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly Buy2DbContext _context;
    public GenericRepository(Buy2DbContext context)
    {
        _context = context;
    }

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params string[] includes)
    {
        return ApplySpecification(new Specification<T>().Where(predicate).Include(includes))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return ApplySpecification(specification).Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public Task<List<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params string[] includes)
    {
        var specification = new Specification<T>().Include(includes);
        if (predicate is not null)
        {
            specification.Where(predicate);
        }

        return ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public Task<List<TResult>> ListAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return ApplySpecification(specification).Select(selector).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(tracked: false, ignoreQueryFilters: false);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // Paging and ordering do not affect the total; criteria only.
        var query = BaseQuery(tracked: false, ignoreQueryFilters: specification.IgnoreQueryFilters);
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _context.Set<T>().AnyAsync(predicate, cancellationToken);
    }

    public Task<int> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default)
    {
        return Filtered(predicate).SumAsync(selector, cancellationToken);
    }

    public Task<int?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default)
    {
        return Filtered(predicate).SumAsync(selector, cancellationToken);
    }

    public Task<decimal> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        return Filtered(predicate).SumAsync(selector, cancellationToken);
    }

    public Task<decimal?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default)
    {
        return Filtered(predicate).SumAsync(selector, cancellationToken);
    }

    public async Task<PagedResult<T>> PagedAsync(ISpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var totalCount = await CountAsync(specification, cancellationToken);

        var query = ApplySpecification(specification, applyPaging: false)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await query.ToListAsync(cancellationToken);
        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await _context.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => await _context.AddRangeAsync(entities, cancellationToken);

    public void Delete(T entity) => _context.Remove(entity);

    public void Update(T entity) => _context.Update(entity);

    private IQueryable<T> Filtered(Expression<Func<T, bool>>? predicate)
    {
        var query = _context.Set<T>().AsNoTracking();
        return predicate is null ? query : query.Where(predicate);
    }

    private IQueryable<T> BaseQuery(bool tracked, bool ignoreQueryFilters)
    {
        IQueryable<T> query = _context.Set<T>();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return query;
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> specification, bool applyPaging = true)
    {
        var query = BaseQuery(specification.Tracked, specification.IgnoreQueryFilters);

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        var ordered = false;
        foreach (var ordering in specification.Orderings)
        {
            if (!ordered)
            {
                query = ordering.Descending
                    ? query.OrderByDescending(ordering.KeySelector)
                    : query.OrderBy(ordering.KeySelector);
                ordered = true;
            }
            else if (query is IOrderedQueryable<T> orderedQuery)
            {
                query = ordering.Descending
                    ? orderedQuery.ThenByDescending(ordering.KeySelector)
                    : orderedQuery.ThenBy(ordering.KeySelector);
            }
        }

        if (applyPaging)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }
}
