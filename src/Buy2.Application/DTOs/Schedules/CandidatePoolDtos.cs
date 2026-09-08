using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.Schedules;

public record ShiftCandidateCardDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string? AvatarUrl,
    string JobTitle,
    int JobRoleId,
    decimal CurrentWeeklyHours,
    decimal RatingScore,
    bool IsPreferredForSite
);

public record ShiftCandidatePreviewDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string? AvatarUrl,
    string JobTitle,
    DateTime JoinDate,
    decimal HourlyRate,
    decimal Rating,
    decimal CareerCompletedHours,
    List<string> Qualifications,
    bool IsPreferredForSite
);

public record PaginatedShiftCandidatesResponseDto(
    List<ShiftCandidateCardDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
