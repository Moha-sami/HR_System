using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class TemplateApplicationLoader : ITemplateApplicationLoader
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftTemplate> _templateRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TemplateApplicationLoader(
        IRepository<Site> siteRepository,
        IRepository<ShiftTemplate> templateRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<JobRole> jobRoleRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _templateRepository = templateRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _jobRoleRepository = jobRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Site?> LoadSiteAsync(int siteId, CancellationToken cancellationToken = default)
    {
        return _siteRepository.FirstOrDefaultAsync(
            s => s.Id == siteId, cancellationToken, nameof(Site.OperationalHours));
    }

    public Task<ShiftTemplate?> LoadTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        return _templateRepository.FirstOrDefaultAsync(
            t => t.Id == templateId, cancellationToken, nameof(ShiftTemplate.ShiftBlocks));
    }

    public Task<List<ShiftEntity>> LoadDayShiftsAsync(
        int siteId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var specification = new Specification<ShiftEntity>()
            .Where(s => s.SiteId == siteId && s.StartTime < dayEnd && s.EndTime > dayStart)
            .OrderBy(s => s.StartTime);

        return _shiftRepository.ListAsync(specification, cancellationToken);
    }

    public async Task<Dictionary<int, Employee>> LoadEmployeesAsync(
        List<ShiftBlock> blocks, CancellationToken cancellationToken = default)
    {
        var ids = blocks
            .Where(b => b.EmployeeId.HasValue)
            .Select(b => b.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, Employee>();
        }

        var employees = await _employeeRepository.ListAsync(
            e => ids.Contains(e.Id), cancellationToken, nameof(Employee.PayrollProfile));

        return employees.ToDictionary(e => e.Id);
    }

    public async Task<Dictionary<int, string>> LoadRoleTitlesAsync(
        List<ShiftBlock> blocks, CancellationToken cancellationToken = default)
    {
        var ids = blocks.Select(b => b.JobRoleId).Distinct().ToList();

        var roles = await _jobRoleRepository.ListAsync(
            r => ids.Contains(r.Id), cancellationToken);

        return roles.ToDictionary(r => r.Id, r => r.Title);
    }

    public async Task<Dictionary<int, Employee>> LoadCostEmployeesAsync(
        List<ShiftEntity> finalDay,
        Dictionary<int, Employee> known,
        CancellationToken cancellationToken = default)
    {
        var missingIds = finalDay
            .Where(s => s.EmployeeId.HasValue && !known.ContainsKey(s.EmployeeId.Value))
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (missingIds.Count == 0)
        {
            return new Dictionary<int, Employee>(known);
        }

        var missing = await _employeeRepository.ListAsync(
            e => missingIds.Contains(e.Id), cancellationToken, nameof(Employee.PayrollProfile));

        foreach (var employee in missing)
        {
            known[employee.Id] = employee;
        }

        return known;
    }

    public async Task<Dictionary<int, decimal>> LoadWeeklyHoursBeforeDayAsync(
        List<ShiftEntity> finalDay, DateOnly date, CancellationToken cancellationToken = default)
    {
        var ids = finalDay
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var (weekStart, _) = GetWeekBoundary(date);
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var shifts = await _shiftRepository.ListAsync(
            s => s.EmployeeId != null
                && ids.Contains(s.EmployeeId.Value)
                && s.StartTime >= weekStart
                && s.StartTime < dayStart,
            cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => shifts
                .Where(s => s.EmployeeId == id)
                .Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours));
    }

    public async Task PersistAsync(IEnumerable<PlannedBlock> planned, CancellationToken cancellationToken = default)
    {
        foreach (var block in planned.Where(p => !p.Pruned))
        {
            await _shiftRepository.AddAsync(block.Entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static (DateTimeOffset WeekStart, DateTimeOffset WeekEnd) GetWeekBoundary(DateOnly targetDate)
    {
        int diff = (7 + (int)targetDate.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = targetDate.AddDays(-diff);
        var sunday = monday.AddDays(6);

        var weekStart = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var weekEnd = new DateTimeOffset(sunday.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return (weekStart, weekEnd);
    }
}
