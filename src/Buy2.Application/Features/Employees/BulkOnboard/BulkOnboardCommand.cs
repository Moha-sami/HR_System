using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.BulkOnboard;

public record BulkOnboardCommand(List<BulkOnboardEmployeeItemDto> Employees) : IRequest<BulkOnboardResultDto>;

public class BulkOnboardCommandHandler : IRequestHandler<BulkOnboardCommand, BulkOnboardResultDto>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkOnboardCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<Role> roleRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _jobRoleRepository = jobRoleRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BulkOnboardResultDto> Handle(BulkOnboardCommand request, CancellationToken cancellationToken)
    {
        if (request.Employees == null || request.Employees.Count == 0)
        {
            return new BulkOnboardResultDto(0, 0, 0, new List<BulkOnboardRowErrorDto>());
        }

        var totalCount = request.Employees.Count;
        var failedRows = new List<BulkOnboardRowErrorDto>();
        var validEmployees = new List<Employee>();

        // 1. Batch query existing roles, job roles, and sites to prevent N+1 queries
        var roles = await _roleRepository.Query().ToListAsync(cancellationToken);
        var roleById = roles.ToDictionary(r => r.Id);
        var roleByName = roles
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var jobRoles = await _jobRoleRepository.Query().ToListAsync(cancellationToken);
        var jobRoleById = jobRoles.ToDictionary(jr => jr.Id);
        var jobRoleByTitle = jobRoles
            .GroupBy(jr => jr.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sites = await _siteRepository.Query().ToListAsync(cancellationToken);
        var siteById = sites.ToDictionary(s => s.Id);
        var siteByName = sites
            .GroupBy(s => s.SiteName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // 2. Batch query existing emails in database
        var incomingEmails = request.Employees
            .Where(e => !string.IsNullOrWhiteSpace(e.Email))
            .Select(e => e.Email.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var existingDbEmails = (await _employeeRepository.Query()
            .Where(e => incomingEmails.Contains(e.Email.ToLower()))
            .Select(e => e.Email.ToLower())
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Track emails seen inside this payload batch to catch duplicates
        var seenPayloadEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 3. Process each row
        for (int i = 0; i < request.Employees.Count; i++)
        {
            var rowIndex = i + 1;
            var item = request.Employees[i];

            if (item == null)
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, null, "Row data is empty."));
                continue;
            }

            var email = item.Email?.Trim();

            // Validate required basic info
            if (string.IsNullOrWhiteSpace(item.FirstName))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, "First name is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.LastName))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, "Last name is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, null, "Email is required."));
                continue;
            }

            if (!EmailRegex.IsMatch(email))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Email format is invalid: '{email}'."));
                continue;
            }

            var normalizedEmail = email.ToLowerInvariant();

            // Check duplicate in current request payload
            if (seenPayloadEmails.Contains(normalizedEmail))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Duplicate email '{email}' in payload."));
                continue;
            }

            // Check duplicate in database
            if (existingDbEmails.Contains(normalizedEmail))
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Employee with email '{email}' already exists."));
                continue;
            }

            // Resolve Role
            int assignedRoleId;
            if (item.RoleId.HasValue && item.RoleId.Value > 0)
            {
                if (!roleById.TryGetValue(item.RoleId.Value, out var matchedRole))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Role ID '{item.RoleId.Value}' not found."));
                    continue;
                }
                assignedRoleId = matchedRole.Id;
            }
            else if (!string.IsNullOrWhiteSpace(item.RoleName))
            {
                if (!roleByName.TryGetValue(item.RoleName.Trim(), out var matchedRole))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Role with name '{item.RoleName}' not found."));
                    continue;
                }
                assignedRoleId = matchedRole.Id;
            }
            else if (roleByName.TryGetValue("Employee", out var defaultRole))
            {
                assignedRoleId = defaultRole.Id;
            }
            else if (roles.Count > 0)
            {
                assignedRoleId = roles[0].Id;
            }
            else
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, "No roles exist in the system to assign."));
                continue;
            }

            // Resolve JobRole
            int assignedJobRoleId;
            if (item.JobRoleId.HasValue && item.JobRoleId.Value > 0)
            {
                if (!jobRoleById.TryGetValue(item.JobRoleId.Value, out var matchedJobRole))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Job Role ID '{item.JobRoleId.Value}' not found."));
                    continue;
                }
                assignedJobRoleId = matchedJobRole.Id;
            }
            else if (!string.IsNullOrWhiteSpace(item.JobTitle))
            {
                if (!jobRoleByTitle.TryGetValue(item.JobTitle.Trim(), out var matchedJobRole))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Job Role title '{item.JobTitle}' not found."));
                    continue;
                }
                assignedJobRoleId = matchedJobRole.Id;
            }
            else if (jobRoles.Count > 0)
            {
                assignedJobRoleId = jobRoles[0].Id;
            }
            else
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, "No job roles exist in the system to assign."));
                continue;
            }

            // Resolve Site
            int assignedSiteId;
            if (item.SiteId.HasValue && item.SiteId.Value > 0)
            {
                if (!siteById.TryGetValue(item.SiteId.Value, out var matchedSite))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Site ID '{item.SiteId.Value}' not found."));
                    continue;
                }
                assignedSiteId = matchedSite.Id;
            }
            else if (!string.IsNullOrWhiteSpace(item.SiteName))
            {
                if (!siteByName.TryGetValue(item.SiteName.Trim(), out var matchedSite))
                {
                    failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, $"Site with name '{item.SiteName}' not found."));
                    continue;
                }
                assignedSiteId = matchedSite.Id;
            }
            else if (sites.Count > 0)
            {
                assignedSiteId = sites[0].Id;
            }
            else
            {
                failedRows.Add(new BulkOnboardRowErrorDto(rowIndex, email, "No sites exist in the system to assign."));
                continue;
            }

            // Mark email as seen in batch
            seenPayloadEmails.Add(normalizedEmail);

            // Hash password securely
            var plainPassword = string.IsNullOrWhiteSpace(item.DefaultPassword) ? "Welcome@123" : item.DefaultPassword.Trim();
            var passwordHash = HashPassword(plainPassword);

            // Generate employee code if not provided
            var employeeCode = string.IsNullOrWhiteSpace(item.EmployeeCode)
                ? $"EMP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
                : item.EmployeeCode.Trim();

            var employee = new Employee
            {
                FirstName = item.FirstName.Trim(),
                LastName = item.LastName.Trim(),
                Email = email,
                PhoneNumber = item.PhoneNumber?.Trim() ?? string.Empty,
                EmployeeCode = employeeCode,
                RoleId = assignedRoleId,
                JobRoleId = assignedJobRoleId,
                SiteId = assignedSiteId,
                DirectManagerId = item.DirectManagerId,
                SeniorityLevel = item.SeniorityLevel?.Trim() ?? string.Empty,
                ExperienceYears = item.ExperienceYears ?? 0,
                JobType = item.JobType?.Trim() ?? "FullTime",
                AttendanceType = item.AttendanceType?.Trim() ?? "OnSite",
                Gender = item.Gender ?? Gender.Male,
                Birthdate = item.Birthdate,
                JoinDate = DateTime.UtcNow,
                PasswordHash = passwordHash
            };

            validEmployees.Add(employee);
        }

        // 4. Save valid employees in batch
        foreach (var emp in validEmployees)
        {
            await _employeeRepository.AddAsync(emp);
        }

        if (validEmployees.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BulkOnboardResultDto(
            TotalCount: totalCount,
            CreatedCount: validEmployees.Count,
            FailedCount: failedRows.Count,
            FailedRows: failedRows
        );
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
