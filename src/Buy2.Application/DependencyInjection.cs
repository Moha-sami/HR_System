using Buy2.Application.Features.Roles.CreateRole;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Buy2.Application;

public static class DependencyInjection
{

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreateRoleCommandValidator).Assembly);

        services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateRoleCommand).Assembly);

                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
        return services;
    }
}
