namespace Buy2.Domain.Entities;

public class User : BaseEntity
{
    public int? EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation Property
    public Employee? Employee { get; set; }
}
