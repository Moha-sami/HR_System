using System.Text;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.ExportViolations;

public record ExportViolationsQuery(
    int EmployeeId,
    string? Type = null,
    string? SeverityLevel = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null
) : IRequest<byte[]?>;

public class ExportViolationsQueryHandler : IRequestHandler<ExportViolationsQuery, byte[]?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<DisciplinaryViolation> _disciplinaryViolationRepository;

    public ExportViolationsQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<DisciplinaryViolation> disciplinaryViolationRepository)
    {
        _employeeRepository = employeeRepository;
        _disciplinaryViolationRepository = disciplinaryViolationRepository;
    }

    public async Task<byte[]?> Handle(ExportViolationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Check if employee exists and is not soft-deleted
        var employee = await _employeeRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Base query for employee disciplinary violations including navigations
        IQueryable<DisciplinaryViolation> query = _disciplinaryViolationRepository.Query()
            .AsNoTracking()
            .Include(v => v.ReportedBy)
            .Include(v => v.ActionTakenBy)
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
                return GenerateCsvBytes(new List<DisciplinaryViolation>());
            }
        }

        // 4. Filter by SeverityLevel (case-insensitive)
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

        // 7. Generate CSV bytes
        return GenerateCsvBytes(violations);
    }

    private static byte[] GenerateCsvBytes(List<DisciplinaryViolation> violations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Violation ID,Violation Type,Severity,Status,Description,Reported By,Action Type,Action Date,Action Taken By,Action Description,Created At");

        foreach (var v in violations)
        {
            var violationId = v.Id.ToString();
            var violationType = v.ViolationType.ToString();
            var severity = v.Severity;
            var status = v.Status.ToString();
            var description = v.Description;

            var reportedByName = "System";
            if (v.ReportedBy != null)
            {
                var fullName = $"{v.ReportedBy.FirstName} {v.ReportedBy.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    reportedByName = fullName;
                }
            }

            var actionType = v.ActionType ?? "N/A";
            var actionDate = v.ActionDate.HasValue ? v.ActionDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";

            var actionTakenByName = "N/A";
            if (v.ActionTakenBy != null)
            {
                var fullName = $"{v.ActionTakenBy.FirstName} {v.ActionTakenBy.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    actionTakenByName = fullName;
                }
            }

            var actionDescription = v.ActionDescription ?? "N/A";
            var createdAt = v.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine($"{EscapeCsv(violationId)},{EscapeCsv(violationType)},{EscapeCsv(severity)},{EscapeCsv(status)},{EscapeCsv(description)},{EscapeCsv(reportedByName)},{EscapeCsv(actionType)},{EscapeCsv(actionDate)},{EscapeCsv(actionTakenByName)},{EscapeCsv(actionDescription)},{EscapeCsv(createdAt)}");
        }

        var bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + contentBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);

        return result;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
