using Buy2.Domain.Entities;

namespace Buy2.Application.DTOs;

public record SiteDto(
    int Id,
    string SiteName,
    double Latitude,
    double Longitude,
    string MacAddress,
    string Address,
    string MapUrl,
    string PhoneNumber,
    string Instructions,
    int RegionId,
    int MaxCapacity
);
