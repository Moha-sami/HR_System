namespace Buy2.Application.DTOs;

public record CreateSiteDto
{
    public string SiteName { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public List<string> MacWhitelist { get; init; } = new List<string>();
}
