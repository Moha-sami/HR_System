using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Infrastructure.Persistence;

public class Buy2DbContext : DbContext
{
    public Buy2DbContext(DbContextOptions<Buy2DbContext> options)
        : base(options)
    {
    }

    public DbSet<AttendanceProfile> AttendanceProfiles => Set<AttendanceProfile>();
    public DbSet<DisciplinaryViolation> DisciplinaryViolations => Set<DisciplinaryViolation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<JobRole> JobRoles => Set<JobRole>();
    public DbSet<PointsRule> PointsRules => Set<PointsRule>();
    public DbSet<PointsTransaction> PointsTransactions => Set<PointsTransaction>();
    public DbSet<RewardItem> RewardItems => Set<RewardItem>();
    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ShiftClaim> ShiftClaims => Set<ShiftClaim>();
    public DbSet<ShiftEntity> ShiftEntities => Set<ShiftEntity>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<PayrollProfile> PayrollProfiles => Set<PayrollProfile>();
    public DbSet<EmployeeSite> EmployeeSites => Set<EmployeeSite>();
    public DbSet<PerformanceMetric> PerformanceMetrics => Set<PerformanceMetric>();
    public DbSet<PerformanceSubmission> PerformanceSubmissions => Set<PerformanceSubmission>();
    public DbSet<EmployeeAchievement> EmployeeAchievements => Set<EmployeeAchievement>();
    public DbSet<EmployeeTask> EmployeeTasks => Set<EmployeeTask>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<SitePreferredEmployee> SitePreferredEmployees => Set<SitePreferredEmployee>();
    public DbSet<SiteOperationalHour> SiteOperationalHours => Set<SiteOperationalHour>();
    public DbSet<SiteDocument> SiteDocuments => Set<SiteDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Buy2DbContext).Assembly);

        modelBuilder.Entity<Role>().HasQueryFilter(r => r.IsActive);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<EmployeeSite>()
            .HasKey(es => new { es.EmployeeId, es.SiteId });
    }
}
