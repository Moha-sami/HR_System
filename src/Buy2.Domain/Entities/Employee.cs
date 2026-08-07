namespace Buy2.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int JobRoleId { get; set; }
    public int RoleId { get; set; }
    public int SiteId { get; set; }
}
