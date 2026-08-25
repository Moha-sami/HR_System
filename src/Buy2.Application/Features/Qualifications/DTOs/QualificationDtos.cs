using System;

namespace Buy2.Application.Features.Qualifications.DTOs;

public record QualificationDto(
    int Id,
    string Name,
    string Category,
    string? Description,
    bool IsActive
);

public record CreateQualificationDto(
    string Name,
    string Category,
    string? Description
);
