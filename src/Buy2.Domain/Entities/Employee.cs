using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public Gender Gender { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public string SeniorityLevel { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string AttendanceType { get; set; } = string.Empty;
    public string? OnlineWorkdaysJson { get; set; }
    public string? OfflineWorkdaysJson { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    // Foreign Keys
    public int JobRoleId { get; set; }
    public int RoleId { get; set; }
    public int SiteId { get; set; }
    public int? DirectManagerId { get; set; }

    // Navigation Properties
    public Employee? DirectManager { get; set; }
    public ICollection<Site> WorkSites { get; set; } = new List<Site>();
    public ICollection<EmployeeSite> EmployeeSites { get; set; } = new List<EmployeeSite>();
    public ICollection<EmployeeAchievement> Achievements { get; set; } = new List<EmployeeAchievement>();
    public PayrollProfile? PayrollProfile { get; set; }
}
