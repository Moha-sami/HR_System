namespace Buy2.Application.Features.ShiftTemplates.DTOs;

public record ShiftTemplateFilterQueryDto(
    string? SearchTerm = null,
    string SortDir = "desc",
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
