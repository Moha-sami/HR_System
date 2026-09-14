namespace Buy2.Application.DTOs.Employees;

// Create / Update payloads for PerformanceMetric master data.
// Target is display-only (shown next to the actual score, never used in calculations).
public record CreatePerformanceMetricDto(
    string Name,
    string Description,
    decimal Target,
    decimal Weight
);

public record UpdatePerformanceMetricDto(
    string Name,
    string Description,
    decimal Target,
    decimal Weight
);

public record PerformanceMetricDto(
    int Id,
    string Name,
    string Description,
    decimal Target,
    decimal Weight
);

// Single evaluation submitted by a manager / HR / admin for one employee + metric.
// Score is the single source of truth (0-100).
public record CreatePerformanceSubmissionDto(
    int MetricId,
    decimal Score,
    string Feedback
);

public record PerformanceSubmissionResultDto(
    int Id,
    int EmployeeId,
    int MetricId,
    decimal Score,
    DateTimeOffset SubmittedAt
);
