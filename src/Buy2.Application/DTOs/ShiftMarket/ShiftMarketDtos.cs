using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.ShiftMarket;

public record ShiftMarketClaimDto(
    int ClaimId,
    int EmployeeId,
    string EmployeeName,
    DateTimeOffset ClaimSubmissionTimestamp,
    decimal ProjectedOvertimeHours,
    string ClaimStatus,
    string? OvertimeJustification
);

public record ShiftMarketPostingDto(
    int ShiftId,
    int SiteId,
    string SiteName,
    string PostingCreator,
    string ShiftTitle,
    DateOnly Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string FormattedTiming,
    int JobRoleId,
    string JobRoleTitle,
    int RequiredHeadcount,
    int RemainingHeadcount,
    int TotalClaimRequestCount,
    string Status,
    List<ShiftMarketClaimDto> Claims
);

public record ShiftMarketPaginatedResponseDto(
    List<ShiftMarketPostingDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
