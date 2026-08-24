using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Buy2.Application.Common.Interfaces;

public interface IBuy2DbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Employee> Employees { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
