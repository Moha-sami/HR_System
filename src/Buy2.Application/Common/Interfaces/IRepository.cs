using Buy2.Application.Common.Specifications;
using System.Linq.Expressions;

namespace Buy2.Application.Common.Interfaces
{
    /// <summary>
    /// Persistence-agnostic repository. Application code composes queries via
    /// predicates, include paths and <see cref="ISpecification{T}"/> — never
    /// IQueryable, so the application layer has no dependency on EF Core.
    /// Include paths use dotted navigation names, e.g. "JobRole.Department".
    /// </summary>
    public interface IRepository<T> where T : class
    {
        // Single-entity reads
        Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params string[] includes);

        Task<TResult?> FirstOrDefaultAsync<TResult>(
            ISpecification<T> specification,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default);

        // Multi-entity reads
        Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default,
            params string[] includes);

        Task<List<TResult>> ListAsync<TResult>(
            ISpecification<T> specification,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default);

        // Aggregates
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default);
        Task<int?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default);
        Task<decimal> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);
        Task<decimal?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default);

        // Paging (specification carries filter + includes + ordering; paging is applied here)
        Task<PagedResult<T>> PagedAsync(ISpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Identity / full-set reads
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        // Writes
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);
    }
}
