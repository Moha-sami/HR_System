using Buy2.Domain.Entities;

namespace Buy2.Application.Common.Interfaces;

public interface IRepository<T> where T: BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}