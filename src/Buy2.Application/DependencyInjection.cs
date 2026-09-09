using Buy2.Application.Features.Points.Automation;
using Buy2.Application.Features.Points.Automation.Evaluators;
using Buy2.Application.Features.ShiftTemplates.UpdateShiftTemplate;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Buy2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        
        services.AddScoped<IAutomationEvaluator, AttendanceAutomationEvaluator>();
        services.AddScoped<IAutomationEvaluator, TaskAutomationEvaluator>();
        services.AddScoped<IAutomationEvaluator, PerformanceAutomationEvaluator>();
        services.AddScoped<IPointsAutomationRunner, PointsAutomationRunner>();
        services.AddScoped<ShiftTemplateUpdateService>();

        return services;
    }
}
