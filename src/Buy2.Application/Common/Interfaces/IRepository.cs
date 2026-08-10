using System.Linq.Expressions;
using Buy2.Domain.Entities;

namespace Buy2.Application.Common.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}