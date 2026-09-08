using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.OnboardEmployee;

public class OnboardEmployeeCommand : IRequest<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? EmployeeCode { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? JobRoleId { get; set; }
    public string? JobTitle { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? DirectManagerId { get; set; }
    public string? SeniorityLevel { get; set; }
    public int? ExperienceYears { get; set; }
    public string? JobType { get; set; }
    public string? AttendanceType { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? Birthdate { get; set; }
    public string? DefaultPassword { get; set; }
}

public class OnboardEmployeeCommandHandler : IRequestHandler<OnboardEmployeeCommand, int>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OnboardEmployeeCommandHandler(
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

    public async Task<int> Handle(OnboardEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            throw new ValidationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            throw new ValidationException("Last name is required.");
        }

        var email = command.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        if (!EmailRegex.IsMatch(email))
        {
            throw new ValidationException($"Email format is invalid: '{email}'.");
        }

        var emailExists = await _employeeRepository.Query()
            .AnyAsync(e => e.Email.ToLower() == email.ToLower(), cancellationToken);

        if (emailExists)
        {
            throw new ValidationException($"Employee with email '{email}' already exists.");
        }

        // Resolve Role
        int assignedRoleId;
        if (command.RoleId.HasValue && command.RoleId.Value > 0)
        {
            var role = await _roleRepository.GetByIdAsync(command.RoleId.Value, cancellationToken);
            if (role == null)
            {
                throw new ValidationException($"Role ID '{command.RoleId.Value}' not found.");
            }
            assignedRoleId = role.Id;
        }
        else if (!string.IsNullOrWhiteSpace(command.RoleName))
        {
            var role = await _roleRepository.Query()
                .FirstOrDefaultAsync(r => r.Name.ToLower() == command.RoleName.Trim().ToLower(), cancellationToken);
            if (role == null)
            {
                throw new ValidationException($"Role with name '{command.RoleName}' not found.");
            }
            assignedRoleId = role.Id;
        }
        else
        {
            var defaultRole = await _roleRepository.Query()
                .FirstOrDefaultAsync(r => r.Name.ToLower() == "employee", cancellationToken);
            if (defaultRole != null)
            {
                assignedRoleId = defaultRole.Id;
            }
            else
            {
                var firstRole = await _roleRepository.Query().FirstOrDefaultAsync(cancellationToken);
                if (firstRole == null)
                {
                    throw new ValidationException("No roles exist in the system to assign.");
                }
                assignedRoleId = firstRole.Id;
            }
        }

        // Resolve JobRole
        int assignedJobRoleId;
        if (command.JobRoleId.HasValue && command.JobRoleId.Value > 0)
        {
            var jobRole = await _jobRoleRepository.GetByIdAsync(command.JobRoleId.Value, cancellationToken);
            if (jobRole == null)
            {
                throw new ValidationException($"Job Role ID '{command.JobRoleId.Value}' not found.");
            }
            assignedJobRoleId = jobRole.Id;
        }
        else if (!string.IsNullOrWhiteSpace(command.JobTitle))
        {
            var jobRole = await _jobRoleRepository.Query()
                .FirstOrDefaultAsync(jr => jr.Title.ToLower() == command.JobTitle.Trim().ToLower(), cancellationToken);
            if (jobRole == null)
            {
                throw new ValidationException($"Job Role title '{command.JobTitle}' not found.");
            }
            assignedJobRoleId = jobRole.Id;
        }
        else
        {
            var firstJobRole = await _jobRoleRepository.Query().FirstOrDefaultAsync(cancellationToken);
            if (firstJobRole == null)
            {
                throw new ValidationException("No job roles exist in the system to assign.");
            }
            assignedJobRoleId = firstJobRole.Id;
        }

        // Resolve Site
        int assignedSiteId;
        if (command.SiteId.HasValue && command.SiteId.Value > 0)
        {
            var site = await _siteRepository.GetByIdAsync(command.SiteId.Value, cancellationToken);
            if (site == null)
            {
                throw new ValidationException($"Site ID '{command.SiteId.Value}' not found.");
            }
            assignedSiteId = site.Id;
        }
        else if (!string.IsNullOrWhiteSpace(command.SiteName))
        {
            var site = await _siteRepository.Query()
                .FirstOrDefaultAsync(s => s.SiteName.ToLower() == command.SiteName.Trim().ToLower(), cancellationToken);
            if (site == null)
            {
                throw new ValidationException($"Site with name '{command.SiteName}' not found.");
            }
            assignedSiteId = site.Id;
        }
        else
        {
            var firstSite = await _siteRepository.Query().FirstOrDefaultAsync(cancellationToken);
            if (firstSite == null)
            {
                throw new ValidationException("No sites exist in the system to assign.");
            }
            assignedSiteId = firstSite.Id;
        }

        // Hash password securely
        var plainPassword = string.IsNullOrWhiteSpace(command.DefaultPassword) ? "Welcome@123" : command.DefaultPassword.Trim();
        var passwordHash = HashPassword(plainPassword);

        // Generate employee code if not provided
        var employeeCode = string.IsNullOrWhiteSpace(command.EmployeeCode)
            ? $"EMP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            : command.EmployeeCode.Trim();

        var employee = new Employee
        {
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Email = email,
            PhoneNumber = command.PhoneNumber?.Trim() ?? string.Empty,
            EmployeeCode = employeeCode,
            RoleId = assignedRoleId,
            JobRoleId = assignedJobRoleId,
            SiteId = assignedSiteId,
            DirectManagerId = command.DirectManagerId,
            SeniorityLevel = command.SeniorityLevel?.Trim() ?? string.Empty,
            ExperienceYears = command.ExperienceYears ?? 0,
            JobType = command.JobType?.Trim() ?? "FullTime",
            AttendanceType = command.AttendanceType?.Trim() ?? "OnSite",
            Gender = command.Gender ?? Gender.Male,
            Birthdate = command.Birthdate,
            JoinDate = DateTime.UtcNow,
            PasswordHash = passwordHash
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}