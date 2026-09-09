using System.ComponentModel.DataAnnotations;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.CreateShiftBlock;

public class CreateShiftBlockCommandHandler : IRequestHandler<CreateShiftBlockCommand, ShiftBlockResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<SiteOperationalHour> _operationalHourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShiftBlockCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<SiteOperationalHour> operationalHourRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _jobRoleRepository = jobRoleRepository;
        _shiftRepository = shiftRepository;
        _operationalHourRepository = operationalHourRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftBlockResponseDto> Handle(CreateShiftBlockCommand request, CancellationToken cancellationToken)
    {
        ValidateShiftTimes(request.StartTime, request.EndTime);

        await ValidateSiteAsync(request.SiteId, cancellationToken);
        var jobRole = await ValidateAndGetJobRoleAsync(request.JobRoleId, cancellationToken);
        await ValidateOperationalHoursAsync(request.SiteId, request.Date, request.StartTime, request.EndTime, cancellationToken);

        var startDto = new DateTimeOffset(request.Date.ToDateTime(request.StartTime), TimeSpan.Zero);
        var endDto = new DateTimeOffset(request.Date.ToDateTime(request.EndTime), TimeSpan.Zero);

        var isDispatched = request.DispatchPolicy != ShiftMarketDispatchPolicy.None;
        var status = isDispatched ? "OpenInMarket" : "Draft";

        var shift = new ShiftEntity
        {
            SiteId = request.SiteId,
            JobRoleId = request.JobRoleId,
            StartTime = startDto,
            EndTime = endDto,
            IsPublished = isDispatched,
            EmployeeId = null
        };

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ShiftBlockResponseDto(
            shift.Id,
            shift.SiteId,
            shift.JobRoleId,
            jobRole.Title,
            shift.StartTime,
            shift.EndTime,
            shift.IsPublished,
            shift.EmployeeId,
            status,
            request.DispatchPolicy,
            new DateTimeOffset(shift.CreatedAt, TimeSpan.Zero)
        );
    }

    private static void ValidateShiftTimes(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ValidationException("Shift start time must be earlier than end time.");
        }
    }

    private async Task ValidateSiteAsync(int siteId, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken);
        if (site == null)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }
    }

    private async Task<JobRole> ValidateAndGetJobRoleAsync(int jobRoleId, CancellationToken cancellationToken)
    {
        var jobRole = await _jobRoleRepository.GetByIdAsync(jobRoleId, cancellationToken);
        if (jobRole == null || !jobRole.IsActive || jobRole.IsDeleted)
        {
            throw new ValidationException($"Job role with ID {jobRoleId} is not active or does not exist.");
        }

        return jobRole;
    }

    private async Task ValidateOperationalHoursAsync(
        int siteId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken)
    {
        var dayOfWeek = date.DayOfWeek;
        var opHour = await _operationalHourRepository.Query(true)
            .FirstOrDefaultAsync(o => o.SiteId == siteId && o.DayOfWeek == dayOfWeek, cancellationToken);

        if (opHour == null)
        {
            return;
        }

        if (!opHour.IsOpen)
        {
            throw new ValidationException($"Site is closed on {dayOfWeek}.");
        }

        if (startTime < opHour.OpenTime || endTime > opHour.CloseTime)
        {
            throw new ValidationException($"Shift time ({startTime:HH\\:mm} - {endTime:HH\\:mm}) falls outside operational hours ({opHour.OpenTime:HH\\:mm} - {opHour.CloseTime:HH\\:mm}).");
        }
    }
}
