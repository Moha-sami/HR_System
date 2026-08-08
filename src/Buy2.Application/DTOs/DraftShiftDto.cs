namespace Buy2.Application.DTOs;

public record DraftShiftDto(int EmployeeId, int JobRoleId, int SiteId, DateTimeOffset StartTime, DateTimeOffset EndTime);
