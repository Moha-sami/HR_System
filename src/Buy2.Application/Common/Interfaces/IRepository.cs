using Buy2.Domain.Entities;
using System.Linq.Expressions;

namespace Buy2.Application.Common.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        IQueryable<T> Query(bool asNoTracking = true);
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);
    }
}