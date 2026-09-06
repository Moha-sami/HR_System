using System.Text.Json;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.UpdatePayrollProfile;

public record UpdatePayrollProfileResult(bool IsSuccess, bool IsNotFound, string? ErrorMessage = null)
{
    public static UpdatePayrollProfileResult Success() => new(true, false);
    public static UpdatePayrollProfileResult NotFound(string message = "Employee not found.") => new(false, true, message);
    public static UpdatePayrollProfileResult BadRequest(string message) => new(false, false, message);
}

public record UpdatePayrollProfileCommand(int EmployeeId, UpdatePayrollProfileDto Dto) : IRequest<UpdatePayrollProfileResult>;

public class UpdatePayrollProfileCommandHandler : IRequestHandler<UpdatePayrollProfileCommand, UpdatePayrollProfileResult>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PayrollProfile> _payrollProfileRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePayrollProfileCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PayrollProfile> payrollProfileRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _payrollProfileRepository = payrollProfileRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePayrollProfileResult> Handle(UpdatePayrollProfileCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query(asNoTracking: false)
            .Include(e => e.EmployeeSites)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null || employee.IsDeleted || !employee.IsActive)
        {
            return UpdatePayrollProfileResult.NotFound($"Employee with ID {request.EmployeeId} was not found.");
        }

        var dto = request.Dto;

        // Upsert PayrollProfile entity
        var payroll = employee.PayrollProfile;
        if (payroll is null)
        {
            payroll = await _payrollProfileRepository.Query(asNoTracking: false)
                .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId, cancellationToken);
        }

        var isNewPayroll = false;
        if (payroll is null)
        {
            payroll = new PayrollProfile
            {
                EmployeeId = employee.Id
            };
            isNewPayroll = true;
        }

        // Map payroll fields if provided
        if (dto.SalaryType.HasValue)
        {
            payroll.SalaryType = dto.SalaryType.Value.ToString();
        }

        if (dto.PayoutPeriod is not null)
        {
            payroll.PayoutPeriod = dto.PayoutPeriod;
        }

        if (dto.PayoutDay.HasValue)
        {
            payroll.PayoutDay = dto.PayoutDay.Value;
        }

        if (dto.WorkWeekStartDay.HasValue)
        {
            payroll.WorkWeekStart = dto.WorkWeekStartDay.Value.ToString();
        }

        if (dto.WorkWeekEndDay.HasValue)
        {
            payroll.WorkWeekEnd = dto.WorkWeekEndDay.Value.ToString();
        }

        if (dto.PaymentAmount.HasValue)
        {
            payroll.PaymentAmount = dto.PaymentAmount.Value;
        }

        if (dto.OvertimeThresholdHours.HasValue)
        {
            payroll.OvertimeThresholdHours = dto.OvertimeThresholdHours.Value;
        }

        if (dto.OvertimeRateMultiplier.HasValue)
        {
            payroll.OvertimeHourlyRate = dto.OvertimeRateMultiplier.Value;
        }

        if (dto.AttendanceType is not null)
        {
            payroll.AttendanceType = dto.AttendanceType;
            employee.AttendanceType = dto.AttendanceType;
        }

        // WorkSite sync
        if (dto.AssignedWorkSiteIds is not null)
        {
            var distinctSiteIds = dto.AssignedWorkSiteIds.Distinct().ToList();

            if (distinctSiteIds.Count > 0)
            {
                var validSiteCount = await _siteRepository.Query()
                    .Where(s => distinctSiteIds.Contains(s.Id))
                    .CountAsync(cancellationToken);

                if (validSiteCount != distinctSiteIds.Count)
                {
                    return UpdatePayrollProfileResult.BadRequest("One or more specified WorkSite IDs do not exist.");
                }
            }

            // Sync EmployeeSite join table
            employee.EmployeeSites.Clear();
            foreach (var siteId in distinctSiteIds)
            {
                employee.EmployeeSites.Add(new EmployeeSite
                {
                    EmployeeId = employee.Id,
                    SiteId = siteId
                });
            }

            payroll.WorkSiteIdsJson = JsonSerializer.Serialize(distinctSiteIds);
        }

        // Online & Offline Workdays JSON serialization
        if (dto.OnlineWorkdays is not null)
        {
            var onlineJson = JsonSerializer.Serialize(dto.OnlineWorkdays);
            employee.OnlineWorkdaysJson = onlineJson;
            payroll.OnlineWorkdaysJson = onlineJson;
        }

        if (dto.OfflineWorkdays is not null)
        {
            var offlineJson = JsonSerializer.Serialize(dto.OfflineWorkdays);
            employee.OfflineWorkdaysJson = offlineJson;
            payroll.OfflineWorkdaysJson = offlineJson;
        }

        if (isNewPayroll)
        {
            await _payrollProfileRepository.AddAsync(payroll, cancellationToken);
            employee.PayrollProfile = payroll;
        }
        else
        {
            _payrollProfileRepository.Update(payroll);
        }

        _employeeRepository.Update(employee);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdatePayrollProfileResult.Success();
    }
}
