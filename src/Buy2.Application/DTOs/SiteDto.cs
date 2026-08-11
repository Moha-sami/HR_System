namespace Buy2.Application.DTOs;

public record SiteDto(
    int Id,
    string SiteName,
    double Latitude,
    double Longitude,
    List<string> MacAddressWhitelist
);
