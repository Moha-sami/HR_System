using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.SetSiteDayOff;

public class SetSiteDayOffCommandHandler : IRequestHandler<SetSiteDayOffCommand, SetSiteDayOffResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<SiteOperationalHour> _operationalHourRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetSiteDayOffCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<SiteOperationalHour> operationalHourRepository,
        IRepository<ShiftEntity> shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _operationalHourRepository = operationalHourRepository;
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SetSiteDayOffResponseDto> Handle(
        SetSiteDayOffCommand request,
        CancellationToken cancellationToken)
    {
        var site = await _siteRepository.GetByIdAsync(request.SiteId, cancellationToken);
        if (site == null)
        {
            throw new KeyNotFoundException($"Site with ID {request.SiteId} not found.");
        }

        await UpdateOrCreateOperationalHourAsync(request.SiteId, request.Date.DayOfWeek, !request.IsDayOff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var existingShifts = await GetExistingShiftsOnDateAsync(request.SiteId, request.Date, cancellationToken);
        var coverageStatus = DetermineCoverageStatus(request.IsDayOff, existingShifts);

        var message = request.IsDayOff
            ? $"Site '{site.SiteName}' marked as day off for {request.Date:yyyy-MM-dd}."
            : $"Site '{site.SiteName}' marked as operational for {request.Date:yyyy-MM-dd}.";

        return new SetSiteDayOffResponseDto(
            SiteId: site.Id,
            SiteName: site.SiteName,
            Date: request.Date,
            DayOfWeek: request.Date.DayOfWeek,
            IsDayOff: request.IsDayOff,
            CoverageStatus: coverageStatus,
            ExistingShiftsCount: existingShifts.Count,
            Message: message
        );
    }

    private async Task UpdateOrCreateOperationalHourAsync(
        int siteId,
        DayOfWeek dayOfWeek,
        bool isOpen,
        CancellationToken cancellationToken)
    {
        var opHour = await _operationalHourRepository.Query(true)
            .FirstOrDefaultAsync(o => o.SiteId == siteId && o.DayOfWeek == dayOfWeek, cancellationToken);

        if (opHour != null)
        {
            opHour.IsOpen = isOpen;
            _operationalHourRepository.Update(opHour);
        }
        else
        {
            var newOpHour = new SiteOperationalHour
            {
                SiteId = siteId,
                DayOfWeek = dayOfWeek,
                IsOpen = isOpen,
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(17, 0)
            };
            await _operationalHourRepository.AddAsync(newOpHour, cancellationToken);
        }
    }

    private async Task<List<ShiftEntity>> GetExistingShiftsOnDateAsync(
        int siteId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var targetDate = date.ToDateTime(TimeOnly.MinValue).Date;
        var windowStart = new DateTimeOffset(targetDate, TimeSpan.Zero).AddDays(-1);
        var windowEnd = new DateTimeOffset(targetDate, TimeSpan.Zero).AddDays(2);

        var siteShifts = await _shiftRepository.Query(true)
            .Where(s => s.SiteId == siteId && s.StartTime >= windowStart && s.StartTime < windowEnd)
            .ToListAsync(cancellationToken);

        return siteShifts
            .Where(s => s.StartTime.Date == targetDate ||
                        DateOnly.FromDateTime(s.StartTime.Date) == date ||
                        DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == date)
            .ToList();
    }

    private static WeekDayCalendarStatus DetermineCoverageStatus(
        bool isDayOff,
        IReadOnlyList<ShiftEntity> shifts)
    {
        if (isDayOff)
        {
            return WeekDayCalendarStatus.DimmedDayOff;
        }

        if (shifts.Count == 0)
        {
            return WeekDayCalendarStatus.NoAllocations;
        }

        if (shifts.Any(s => s.EmployeeId == null || !s.IsPublished))
        {
            return WeekDayCalendarStatus.MissingResourcesOrUnpublished;
        }

        return WeekDayCalendarStatus.CoveredAndPublished;
    }
}
