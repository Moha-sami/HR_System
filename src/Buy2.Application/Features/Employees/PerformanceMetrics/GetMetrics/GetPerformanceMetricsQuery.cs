using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;

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
        var spec = new Specification<PerformanceMetric>()
            .OrderBy(m => m.Name);
        var metrics = await _metricRepository.ListAsync(spec, cancellationToken);
        return metrics.Select(m => new PerformanceMetricDto(
                m.Id,
                m.Name,
                m.Description,
                m.Target,
                m.Weight))
            .ToList();
    }
}
