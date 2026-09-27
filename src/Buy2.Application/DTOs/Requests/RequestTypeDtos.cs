using System;

namespace Buy2.Application.DTOs.Requests;

public record RequestTypeDto(
    int Id,
    string Category,
    string Name,
    string? Hint,
    string? LeaveType,
    string? LeavePay,
    bool RequiresDates,
    bool RequiresReason,
    bool IsActive,
    DateTime CreatedAt,
    string? AddedBy,
    bool IsUtilized = false
);

public record CreateRequestTypeDto(
    string Category,
    string Name,
    string? Hint,
    string? LeaveType,
    string? LeavePay,
    bool RequiresDates,
    bool RequiresReason,
    bool IsActive = true,
    string? AddedBy = null
);

public record UpdateRequestTypeDto(
    string Category,
    string Name,
    string? Hint,
    string? LeaveType,
    string? LeavePay,
    bool RequiresDates,
    bool RequiresReason,
    bool IsActive
);
