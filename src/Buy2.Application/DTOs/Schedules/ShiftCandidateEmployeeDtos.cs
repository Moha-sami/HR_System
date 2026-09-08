using System.Collections.Generic;

namespace Buy2.Application.DTOs.Schedules;

public record ShiftCandidateEmployeeItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string RoleTitle,
    int JobRoleId,
    decimal WeeklyCompletedHours,
    decimal RatingScore,
    string RiskStatusToken,
    bool IsPreferredForSite
);

public record PaginatedShiftCandidateEmployeesResponseDto(
    List<ShiftCandidateEmployeeItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
