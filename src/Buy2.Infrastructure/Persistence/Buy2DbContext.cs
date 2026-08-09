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
    public DbSet<Shift> ShiftEntities => Set<Shift>();
    public DbSet<Site> Sites => Set<Site>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Buy2DbContext).Assembly);
    }
}
