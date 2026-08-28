using System;

namespace Buy2.Application.Features.Qualifications.DTOs;

public record QualificationDto(
    int Id,
    string Name,
    string Type,
    string? Description,
    bool IsActive
);

public record CreateQualificationDto(
    string Name,
    string Type,
    string? Description
);

public record QualificationLookupDto(
    int Id,
    string Name,
    string Type,
    string? Description,
    bool IsSystem
);
