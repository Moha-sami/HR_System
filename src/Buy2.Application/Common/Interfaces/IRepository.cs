using Buy2.Domain.Entities;

namespace Buy2.Application.Common.Interfaces;

public interface IRepository<T> where T: BaseEntity
{
    public Task<T?> GetByIdAsync(int id);
    public Task<IEnumerable<T>> GetAllAsync();
    public Task AddAsync(T entity);
    public void Update(T entity);
    public void Delete(T entity);
}