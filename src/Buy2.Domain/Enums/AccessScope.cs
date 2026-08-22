namespace Buy2.Domain.Enums;

/// <summary>
/// Defines the access scope boundary for role permissions.
/// </summary>
public enum AccessScope
{
    All = 1,
    Department = 2,
    Region = 3,
    Site = 4,
    Team = 5,
    Specific = 6
}
