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
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public string? NationalId { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public string SeniorityLevel { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string AttendanceType { get; set; } = string.Empty;
    public string? OnlineWorkdaysJson { get; set; }
    public string? OfflineWorkdaysJson { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }

    // Foreign Keys
    public int JobRoleId { get; set; }
    public int RoleId { get; set; }
    public int SiteId { get; set; }
    public int? DirectManagerId { get; set; }

    // Navigation Properties
    public JobRole? JobRole { get; set; }
    public Role? Role { get; set; }
    public Site? Site { get; set; }
    public Employee? DirectManager { get; set; }
    public ICollection<Site> WorkSites { get; set; } = new List<Site>();
    public ICollection<EmployeeSite> EmployeeSites { get; set; } = new List<EmployeeSite>();
    public ICollection<EmployeeAchievement> Achievements { get; set; } = new List<EmployeeAchievement>();
    public ICollection<EmployeeTask> Tasks { get; set; } = new List<EmployeeTask>();
    public PayrollProfile? PayrollProfile { get; set; }
}
