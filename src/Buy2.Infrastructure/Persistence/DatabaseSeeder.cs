using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly string[] FirstNames = ["Mohamed", "Ahmed", "Mahmoud", "Omar", "Youssef", "Ali", "Hassan", "Ibrahim", "Mostafa", "Khaled", "Sarah", "Nour", "Nerveen", "Dina", "Fatma"];
    private static readonly string[] LastNames = ["Sami", "Shalapy", "Elshahawy", "Alim", "Gamal", "Hassan", "Ibrahim", "Fawzy", "Kamel", "Mansour", "Soliman", "Nasser", "Amer"];

    public static async Task SeedAsync(Buy2DbContext context)
    {
        // 1. Ensure Database Created
        await context.Database.EnsureCreatedAsync();

        // 2. Ensure Default Roles Exist
        if (!await context.Set<Role>().AnyAsync())
        {
            context.Set<Role>().AddRange(
                new Role { Name = "Admin", PermissionsJson = "[\"all\"]" },
                new Role { Name = "Manager", PermissionsJson = "[\"manage_schedules\", \"approve_claims\"]" },
                new Role { Name = "Employee", PermissionsJson = "[\"view_schedules\", \"claim_shifts\"]" }
            );
            await context.SaveChangesAsync();
        }

        // 3. Ensure Default Sites Exist
        if (!await context.Set<Site>().AnyAsync())
        {
            context.Set<Site>().AddRange(
                new Site { SiteName = "Cairo HQ", Latitude = 30.0444, Longitude = 31.2357, MacAddressWhitelistJson = "[\"00:1A:2B:3C:4D:5E\"]" },
                new Site { SiteName = "Alexandria Hub", Latitude = 31.2001, Longitude = 29.9187, MacAddressWhitelistJson = "[\"00:1A:2B:3C:4D:5F\"]" }
            );
            await context.SaveChangesAsync();
        }

        // 4. Ensure Default JobRoles Exist
        if (!await context.Set<JobRole>().AnyAsync())
        {
            var defaultSite = await context.Set<Site>().FirstAsync();
            context.Set<JobRole>().AddRange(
                new JobRole { Title = "Software Engineer", DepartmentId = 1, RequiredQualificationsJson = "[\"C#\", \".NET\"]" },
                new JobRole { Title = "HR Specialist", DepartmentId = 2, RequiredQualificationsJson = "[\"Communication\"]" },
                new JobRole { Title = "Operations Lead", DepartmentId = 3, RequiredQualificationsJson = "[\"Management\"]" }
            );
            await context.SaveChangesAsync();
        }

        // 5. Seed 100,000 Employees in Fast Batches of 5,000
        int existingCount = await context.Set<Employee>().CountAsync();
        int targetTotal = 100000;
        int remainingToSeed = targetTotal - existingCount;

        if (remainingToSeed <= 0)
        {
            return;
        }

        var roles = await context.Set<Role>().ToListAsync();
        var sites = await context.Set<Site>().ToListAsync();
        var jobRoles = await context.Set<JobRole>().ToListAsync();

        int defaultRoleId = roles.First().Id;
        int defaultSiteId = sites.First().Id;
        int defaultJobRoleId = jobRoles.First().Id;

        int batchSize = 5000;
        int seededSoFar = 0;

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        while (seededSoFar < remainingToSeed)
        {
            int currentBatchCount = Math.Min(batchSize, remainingToSeed - seededSoFar);
            var batchEmployees = new List<Employee>(currentBatchCount);

            for (int i = 0; i < currentBatchCount; i++)
            {
                int globalIndex = existingCount + seededSoFar + i + 1;
                string fname = FirstNames[globalIndex % FirstNames.Length];
                string lname = LastNames[globalIndex % LastNames.Length];

                batchEmployees.Add(new Employee
                {
                    FirstName = fname,
                    LastName = lname,
                    Email = $"employee_{globalIndex}@buy2hrms.com",
                    PhoneNumber = $"+2010{10000000 + (globalIndex % 90000000)}",
                    RoleId = roles[globalIndex % roles.Count].Id,
                    SiteId = sites[globalIndex % sites.Count].Id,
                    JobRoleId = jobRoles[globalIndex % jobRoles.Count].Id
                });
            }

            await context.Set<Employee>().AddRangeAsync(batchEmployees);
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();
            seededSoFar += currentBatchCount;
        }

        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
