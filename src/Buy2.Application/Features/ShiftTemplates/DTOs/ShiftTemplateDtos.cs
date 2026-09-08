namespace Buy2.Application.Features.ShiftTemplates.DTOs;

public record ShiftTemplateFilterQueryDto(
    string? SearchTerm = null,
    string? SortDir = null,
    string? NameSort = null,
    string? CreationSort = null,
    string? UpdatedSort = null,
    string? NumberOfAssignedSort = null,
    int PageNumber = 1,
    int PageSize = 10
);

public record ShiftTemplateListItemDto(
    int Id,
    string Name,
    string CreationDate,
    string LastUpdated,
    int NumberOfAssignedSites
);

public record ShiftTemplatePaginatedResponseDto<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public record CreateShiftTemplateBlockDto(
    string StartTime,
    string EndTime,
    int JobRoleId,
    int AssignedUserId
);

public record CreateShiftTemplateDto(
    string Name,
    List<int> SiteIds,
    string StartTime,
    string EndTime,
    List<CreateShiftTemplateBlockDto> ShiftBlocks
);

public record ShiftTemplateSiteItemDto(
    int SiteId,
    string SiteName
);

public record ShiftTemplateBlockDetailsDto(
    int Id,
    string StartTime,
    string EndTime,
    int JobRoleId,
    string JobRoleTitle,
    int AssignedUserId,
    string AssignedUserName
);

public record ShiftTemplateDetailsDto(
    int Id,
    string Name,
    string StartTime,
    string EndTime,
    string CreationDate,
    string LastUpdated,
    int NumberOfAssignedSites,
    List<ShiftTemplateSiteItemDto> Sites,
    List<ShiftTemplateBlockDetailsDto> ShiftBlocks
);
