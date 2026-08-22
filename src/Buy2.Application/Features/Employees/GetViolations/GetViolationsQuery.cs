using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetViolations;

public record GetViolationsQuery(
    int EmployeeId,
    string? Type = null,
    string? SeverityLevel = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null
) : IRequest<List<ViolationDto>?>;

public class GetViolationsQueryHandler : IRequestHandler<GetViolationsQuery, List<ViolationDto>?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<DisciplinaryViolation> _disciplinaryViolationRepository;

    public GetViolationsQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<DisciplinaryViolation> disciplinaryViolationRepository)
    {
        _employeeRepository = employeeRepository;
        _disciplinaryViolationRepository = disciplinaryViolationRepository;
    }

    public async Task<List<ViolationDto>?> Handle(GetViolationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Check if employee exists and is not soft-deleted
        var employee = await _employeeRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Base query for employee disciplinary violations including ReportedBy navigation
        var query = _disciplinaryViolationRepository.Query()
            .Include(v => v.ReportedBy)
            .Where(v => v.EmployeeId == request.EmployeeId);

        // 3. Filter by Type
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var typeStr = request.Type.Trim();
            if (Enum.TryParse<ViolationType>(typeStr, ignoreCase: true, out var violationTypeEnum))
            {
                query = query.Where(v => v.ViolationType == violationTypeEnum);
            }
            else
            {
                return new List<ViolationDto>();
            }
        }

        // 4. Filter by SeverityLevel (case-insensitive comparison)
        if (!string.IsNullOrWhiteSpace(request.SeverityLevel))
        {
            var severityLevel = request.SeverityLevel.Trim().ToLower();
            query = query.Where(v => v.Severity.ToLower() == severityLevel);
        }

        // 5. Filter by Date range
        if (request.DateFrom.HasValue)
        {
            var fromUtc = request.DateFrom.Value.UtcDateTime;
            query = query.Where(v => v.CreatedAt >= fromUtc);
        }

        if (request.DateTo.HasValue)
        {
            var toUtc = request.DateTo.Value.UtcDateTime;
            query = query.Where(v => v.CreatedAt <= toUtc);
        }

        // 6. Order by CreatedAt descending
        var violations = await query
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        // 7. Map items cleanly into DTOs
        return violations.Select(MapToDto).ToList();
    }

    private static ViolationDto MapToDto(DisciplinaryViolation v)
    {
        var reportedByName = "System";
        if (v.ReportedBy != null)
        {
            var fullName = $"{v.ReportedBy.FirstName} {v.ReportedBy.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                reportedByName = fullName;
            }
        }

        var createdAtUtc = DateTime.SpecifyKind(v.CreatedAt, DateTimeKind.Utc);

        return new ViolationDto(
            Id: v.Id,
            EmployeeId: v.EmployeeId,
            Type: v.ViolationType.ToString(),
            Severity: v.Severity,
            Description: v.Description,
            Status: v.Status.ToString(),
            ReportedByName: reportedByName,
            CreatedAt: new DateTimeOffset(createdAtUtc),
            ActionType: v.ActionType,
            ActionDate: v.ActionDate,
            DocumentUrl: v.DocumentUrl
        );
    }
}
