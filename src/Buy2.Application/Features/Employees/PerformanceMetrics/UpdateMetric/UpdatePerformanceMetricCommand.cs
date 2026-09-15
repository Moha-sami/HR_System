using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Employees;
using Buy2.Application.Validators.Employees;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.PerformanceMetrics.UpdateMetric;

public record UpdatePerformanceMetricCommand(int MetricId, UpdatePerformanceMetricDto Dto) : IRequest<Result<PerformanceMetricDto>>;

public class UpdatePerformanceMetricCommandHandler : IRequestHandler<UpdatePerformanceMetricCommand, Result<PerformanceMetricDto>>
{
    private readonly IRepository<PerformanceMetric> _metricRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePerformanceMetricCommandHandler(
        IRepository<PerformanceMetric> metricRepository,
        IUnitOfWork unitOfWork)
    {
        _metricRepository = metricRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PerformanceMetricDto>> Handle(UpdatePerformanceMetricCommand command, CancellationToken cancellationToken)
    {
        if (command.Dto is null)
        {
            return Result<PerformanceMetricDto>.ValidationFailure("Metric data is required.");
        }

        var validationResult = await new UpdatePerformanceMetricDtoValidator().ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PerformanceMetricDto>.ValidationFailure(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var metric = await _metricRepository.GetByIdAsync(command.MetricId, cancellationToken);
        if (metric is null)
        {
            return Result<PerformanceMetricDto>.NotFound($"Metric with ID {command.MetricId} was not found.");
        }

        var nameTaken = await _metricRepository.AnyAsync(
            m => m.Id != command.MetricId && m.Name == command.Dto.Name, cancellationToken);
        if (nameTaken)
        {
            return Result<PerformanceMetricDto>.Conflict($"A metric named '{command.Dto.Name}' already exists.");
        }

        metric.Name = command.Dto.Name.Trim();
        metric.Description = command.Dto.Description?.Trim() ?? string.Empty;
        metric.Target = command.Dto.Target;
        metric.Weight = command.Dto.Weight;

        _metricRepository.Update(metric);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PerformanceMetricDto>.Success(new PerformanceMetricDto(
            metric.Id,
            metric.Name,
            metric.Description,
            metric.Target,
            metric.Weight));
    }
}
