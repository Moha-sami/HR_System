using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.PerformanceMetrics.GetMetrics;

public record GetPerformanceMetricsQuery : IRequest<List<PerformanceMetricDto>>;

public class GetPerformanceMetricsQueryHandler : IRequestHandler<GetPerformanceMetricsQuery, List<PerformanceMetricDto>>
{
    private readonly IRepository<PerformanceMetric> _metricRepository;

    public GetPerformanceMetricsQueryHandler(IRepository<PerformanceMetric> metricRepository)
    {
        _metricRepository = metricRepository;
    }

    public async Task<List<PerformanceMetricDto>> Handle(GetPerformanceMetricsQuery request, CancellationToken cancellationToken)
    {
        return await _metricRepository
            .Query()
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new PerformanceMetricDto(
                m.Id,
                m.Name,
                m.Description,
                m.Target,
                m.Weight))
            .ToListAsync(cancellationToken);
    }
}
