using System.Text.Json;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetViolationDetail;

public record GetViolationDetailQuery(
    int EmployeeId,
    int ViolationId
) : IRequest<ViolationDetailDto?>;

public class GetViolationDetailQueryHandler : IRequestHandler<GetViolationDetailQuery, ViolationDetailDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<DisciplinaryViolation> _disciplinaryViolationRepository;

    public GetViolationDetailQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<DisciplinaryViolation> disciplinaryViolationRepository)
    {
        _employeeRepository = employeeRepository;
        _disciplinaryViolationRepository = disciplinaryViolationRepository;
    }

    public async Task<ViolationDetailDto?> Handle(GetViolationDetailQuery request, CancellationToken cancellationToken)
    {
        // 1. Check if employee exists and is not soft-deleted
        var employee = await _employeeRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Query violation for this employee including navigations
        var violation = await _disciplinaryViolationRepository.Query()
            .AsNoTracking()
            .Include(v => v.ReportedBy)
            .Include(v => v.ActionTakenBy)
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId && v.EmployeeId == request.EmployeeId, cancellationToken);

        if (violation == null)
        {
            return null;
        }

        // 3. Parse WitnessesJson safely
        var witnesses = ParseJsonList(violation.WitnessesJson);

        // 4. Build ActionDetail (null if Status is Pending or no action has been taken)
        ViolationActionDetailDto? actionDetail = null;
        if (violation.Status != ViolationStatus.Pending && HasActionDetails(violation))
        {
            string? actionTakenByName = null;
            if (violation.ActionTakenBy != null)
            {
                var fullName = $"{violation.ActionTakenBy.FirstName} {violation.ActionTakenBy.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    actionTakenByName = fullName;
                }
            }

            actionDetail = new ViolationActionDetailDto(
                ActionType: violation.ActionType,
                ActionDate: violation.ActionDate,
                ActionTakenByName: actionTakenByName,
                ActionDescription: violation.ActionDescription
            );
        }

        // 5. Resolve ReportedByName with default "System"
        var reportedByName = "System";
        if (violation.ReportedBy != null)
        {
            var fullName = $"{violation.ReportedBy.FirstName} {violation.ReportedBy.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                reportedByName = fullName;
            }
        }

        var createdAtUtc = DateTime.SpecifyKind(violation.CreatedAt, DateTimeKind.Utc);

        return new ViolationDetailDto(
            Id: violation.Id,
            EmployeeId: violation.EmployeeId,
            ViolationType: violation.ViolationType.ToString(),
            Severity: violation.Severity,
            Description: violation.Description,
            Status: violation.Status.ToString(),
            ReportedByName: reportedByName,
            Witnesses: witnesses,
            DocumentUrl: violation.DocumentUrl,
            CreatedAt: new DateTimeOffset(createdAtUtc),
            ActionDetail: actionDetail
        );
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json);
            return parsed ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static bool HasActionDetails(DisciplinaryViolation v)
    {
        return !string.IsNullOrWhiteSpace(v.ActionType)
            || v.ActionDate.HasValue
            || v.ActionTakenById.HasValue
            || !string.IsNullOrWhiteSpace(v.ActionDescription);
    }
}
