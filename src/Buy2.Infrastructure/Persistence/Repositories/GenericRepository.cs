using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly Buy2DbContext _context;
    public GenericRepository(Buy2DbContext context)
    {
        _context = context;
    }
    public async Task AddAsync(T entity) => await _context.AddAsync(entity);
 
    

    public void Delete(T entity) => _context.Remove(entity);


    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public void Update(T entity) => _context.Update(entity);    

}
