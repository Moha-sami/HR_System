using Buy2.Domain.Entities;

namespace Buy2.Application.DTOs.Sites;
public record CreateUpdateSiteDto(
    string SiteName,
    double Latitude,
    double Longitude,
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
    DayOfWeek DayOfWeek,
    bool IsOpen,
    DateTimeOffset OpenTime,
    DateTimeOffset CloseTime
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
