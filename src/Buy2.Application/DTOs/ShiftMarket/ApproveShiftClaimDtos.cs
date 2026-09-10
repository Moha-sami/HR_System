namespace Buy2.Application.DTOs.ShiftMarket;

public record ApproveShiftClaimResponseDto(
    int ClaimId,
    int ShiftId,
    int EmployeeId,
    string EmployeeName,
    string ClaimStatus,
    string AssignmentOutcome,
    decimal ProjectedOvertimeHours,
    bool RequiresHrApproval,
    int RemainingHeadcount,
    string PostingStatus,
    string Message
);
