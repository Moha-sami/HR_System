using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using System.Linq.Expressions;

namespace Buy2.Application.Common.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query(bool asNoTracking = true);
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);

        // TEMPORARY default implementations for the stacked review of SCRUM-389.
        // The specification-based reads are implemented by GenericRepository;
        // the follow-up (SCRUM-390) turns these into abstract members and removes Query().
        // Existing IRepository implementers (e.g. test fakes) keep compiling unaffected.
        Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(FirstOrDefaultAsync)} is implemented by GenericRepository.");

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params string[] includes) =>
            throw new NotImplementedException($"{nameof(FirstOrDefaultAsync)} is implemented by GenericRepository.");

        Task<TResult?> FirstOrDefaultAsync<TResult>(
            ISpecification<T> specification,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(FirstOrDefaultAsync)} is implemented by GenericRepository.");

        Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(ListAsync)} is implemented by GenericRepository.");

        Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default,
            params string[] includes) =>
            throw new NotImplementedException($"{nameof(ListAsync)} is implemented by GenericRepository.");

        Task<List<TResult>> ListAsync<TResult>(
            ISpecification<T> specification,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(ListAsync)} is implemented by GenericRepository.");

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(CountAsync)} is implemented by GenericRepository.");

        Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(CountAsync)} is implemented by GenericRepository.");

        Task<int> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(SumAsync)} is implemented by GenericRepository.");

        Task<int?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(SumAsync)} is implemented by GenericRepository.");

        Task<decimal> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(SumAsync)} is implemented by GenericRepository.");

        Task<decimal?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(SumAsync)} is implemented by GenericRepository.");

        Task<PagedResult<T>> PagedAsync(ISpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException($"{nameof(PagedAsync)} is implemented by GenericRepository.");
    }
}
