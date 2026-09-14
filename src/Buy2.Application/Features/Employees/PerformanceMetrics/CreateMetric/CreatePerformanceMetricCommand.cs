using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Employees;
using Buy2.Application.Validators.Employees;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.PerformanceMetrics.CreateMetric;

public record CreatePerformanceMetricCommand(CreatePerformanceMetricDto Dto) : IRequest<Result<PerformanceMetricDto>>;

public class CreatePerformanceMetricCommandHandler : IRequestHandler<CreatePerformanceMetricCommand, Result<PerformanceMetricDto>>
{
    private readonly IRepository<PerformanceMetric> _metricRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePerformanceMetricCommandHandler(
        IRepository<PerformanceMetric> metricRepository,
        IUnitOfWork unitOfWork)
    {
        _metricRepository = metricRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PerformanceMetricDto>> Handle(CreatePerformanceMetricCommand command, CancellationToken cancellationToken)
    {
        if (command.Dto is null)
        {
            return Result<PerformanceMetricDto>.ValidationFailure("Metric data is required.");
        }

        var validationResult = await new CreatePerformanceMetricDtoValidator().ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PerformanceMetricDto>.ValidationFailure(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var nameExists = await _metricRepository
            .Query(false)
            .AnyAsync(m => m.Name == command.Dto.Name, cancellationToken);
        if (nameExists)
        {
            return Result<PerformanceMetricDto>.Conflict($"A metric named '{command.Dto.Name}' already exists.");
        }

        var metric = new PerformanceMetric
        {
            Name = command.Dto.Name.Trim(),
            Description = command.Dto.Description?.Trim() ?? string.Empty,
            Target = command.Dto.Target,
            Weight = command.Dto.Weight
        };

        await _metricRepository.AddAsync(metric, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PerformanceMetricDto>.Success(new PerformanceMetricDto(
            metric.Id,
            metric.Name,
            metric.Description,
            metric.Target,
            metric.Weight));
    }
}
