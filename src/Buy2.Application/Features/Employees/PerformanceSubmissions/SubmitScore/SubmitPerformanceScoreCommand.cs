using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Employees;
using Buy2.Application.Validators.Employees;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.PerformanceSubmissions.SubmitScore;

public record SubmitPerformanceScoreCommand(int EmployeeId, CreatePerformanceSubmissionDto Dto) : IRequest<Result<PerformanceSubmissionResultDto>>;

public class SubmitPerformanceScoreCommandHandler : IRequestHandler<SubmitPerformanceScoreCommand, Result<PerformanceSubmissionResultDto>>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PerformanceMetric> _metricRepository;
    private readonly IRepository<PerformanceSubmission> _submissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitPerformanceScoreCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PerformanceMetric> metricRepository,
        IRepository<PerformanceSubmission> submissionRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _metricRepository = metricRepository;
        _submissionRepository = submissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PerformanceSubmissionResultDto>> Handle(SubmitPerformanceScoreCommand command, CancellationToken cancellationToken)
    {
        if (command.Dto is null)
        {
            return Result<PerformanceSubmissionResultDto>.ValidationFailure("Submission data is required.");
        }

        var validationResult = await new CreatePerformanceSubmissionDtoValidator().ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PerformanceSubmissionResultDto>.ValidationFailure(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.IsDeleted)
        {
            return Result<PerformanceSubmissionResultDto>.NotFound($"Employee with ID {command.EmployeeId} was not found.");
        }

        var metric = await _metricRepository.GetByIdAsync(command.Dto.MetricId, cancellationToken);
        if (metric is null)
        {
            return Result<PerformanceSubmissionResultDto>.NotFound($"Metric with ID {command.Dto.MetricId} was not found.");
        }

        var submission = new PerformanceSubmission
        {
            EmployeeId = command.EmployeeId,
            MetricId = command.Dto.MetricId,
            Score = command.Dto.Score,
            // Transitional sync: Score is the single source of truth.
            // AchievedPercent mirrors it until the column is retired.
            AchievedPercent = command.Dto.Score,
            SubmissionDate = DateTime.UtcNow,
            Feedback = command.Dto.Feedback?.Trim() ?? string.Empty
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PerformanceSubmissionResultDto>.Success(new PerformanceSubmissionResultDto(
            submission.Id,
            submission.EmployeeId,
            submission.MetricId,
            submission.Score,
            new DateTimeOffset(DateTime.SpecifyKind(submission.SubmissionDate, DateTimeKind.Utc))));
    }
}
