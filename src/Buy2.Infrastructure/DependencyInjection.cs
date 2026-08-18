using Buy2.Application.Common.Interfaces;
using Buy2.Infrastructure.Authentication;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Buy2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buy2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
       
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=.;Database=HrSystemDb;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<Buy2DbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddTransient<ExcelVoucherParser>();
        services.AddScoped<IScheduleValidationEngine, ScheduleValidationEngine>();

        return services;
    }
}
