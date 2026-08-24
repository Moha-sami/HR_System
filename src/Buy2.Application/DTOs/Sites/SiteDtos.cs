namespace Buy2.Application.DTOs.Sites;
public record CreateUpdateSiteDto(
    string SiteName,
    double Latitude,
    double Longitude,
    List<string> MacWhitelist,
    string MacAddress,
    string Address,
    string MapUrl,
    string PhoneNumber,
    string Instructions,
    int RegionId,
    int MaxCapacity,
    List<int> PreferredEmployeeIds,
    List<SiteOperationalHourDto> OperationalHours
);
public record SiteOperationalHourDto
(
    DayOfWeek Day,
    bool IsOpen,
    TimeOnly From,
    TimeOnly To
);
public record RegionListItemDto(
    int Id, string Name
);
public record CreateRegionDto(string Name);
public record SiteListItemDto(
    int Id,
    string SiteName,
    string RegionName,
    string Address,
    int OperationalDaysCount,
    double TotalOperationalHours,
    int MaxCapacity
);
public record SiteListResponseDto(
    List<SiteListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// Setp 4
public record SiteFullProfile(
    string SiteName,
    string RegionName,
    string Address,
    string MacAddress,
    string MapUrl,
    string PhoneNumber,
    string Instructions,
    List<PreferredPersonDto> PreferredPeople,
    List<DocumentDto> Documents,
    List<OperationalScheduleDto> OperationalSchedule
);
public record PreferredPersonDto(string Name, string RoleTitle);
public record DocumentDto(int Id, string FileName, string Url);
public record OperationalScheduleDto(DayOfWeek Day, TimeOnly From, TimeOnly To);

public record ShiftTabDto(
    int ShiftId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsSmartPostingEnabled,
    List<ShiftRoleHeadcountDto> Roles
);
public record ShiftRoleHeadcountDto(
    string RoleName,
    int RequiredHeadcount
);

public record EmployeeTabDto(
    int EmployeeId,
    string FullName,
    string RoleName,
    string Email,
    string PhoneNumber,
    string Status
);

// step 5
public record DeletionCheckDto(
    bool CanDelete,
    int AllocatedEmployeesCount,
    List<AllocatedEmployeeDto> AllocatedEmployees
);
public record AllocatedEmployeeDto(int EmployeeId, string FullName);
public record ReallocateAndDeleteSiteDto(
    List<EmployeeSiteReassignmentDto> EmployeeSiteReassignments
);
public record EmployeeSiteReassignmentDto(int EmployeeId, int NewSiteId);