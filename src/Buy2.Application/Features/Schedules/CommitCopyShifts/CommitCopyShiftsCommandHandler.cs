using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.CommitCopyShifts;

public class CommitCopyShiftsCommandHandler : IRequestHandler<CommitCopyShiftsCommand, CommitCopyShiftsResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CommitCopyShiftsCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommitCopyShiftsResponseDto> Handle(
        CommitCopyShiftsCommand request,
        CancellationToken cancellationToken)
    {
        await ValidateSiteExistsAsync(request.SiteId, cancellationToken);
        var targetDates = ResolveTargetDates(request);
        var sourceShifts = await LoadSourceShiftsAsync(request.SiteId, request.SourceDate, cancellationToken);

        return await ExecuteCommitAsync(request, targetDates, sourceShifts, cancellationToken);
    }

    private async Task ValidateSiteExistsAsync(int siteId, CancellationToken cancellationToken)
    {
        var siteExists = await _siteRepository.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }
    }

    private static List<DateOnly> ResolveTargetDates(CommitCopyShiftsCommand request)
    {
        var dates = new HashSet<DateOnly>();
        AddValidDates(dates, request.TargetDates, request.SourceDate);

        if (request.DateResolutions != null)
        {
            AddValidDates(dates, request.DateResolutions.Keys, request.SourceDate);
        }

        if (dates.Count == 0)
        {
            throw new ArgumentException("At least one target date must be specified.");
        }

        return dates.OrderBy(d => d).ToList();
    }

    private static void AddValidDates(
        HashSet<DateOnly> set,
        IEnumerable<DateOnly>? dates,
        DateOnly sourceDate)
    {
        if (dates == null)
        {
            return;
        }

        foreach (var date in dates)
        {
            if (date != sourceDate)
            {
                set.Add(date);
            }
        }
    }

    private async Task<List<ShiftEntity>> LoadSourceShiftsAsync(
        int siteId,
        DateOnly sourceDate,
        CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(sourceDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var shifts = await _shiftRepository.Query(true)
            .Where(s => s.SiteId == siteId && s.StartTime >= dayStart.AddDays(-1) && s.StartTime < dayEnd.AddDays(1))
            .ToListAsync(cancellationToken);

        var sourceShifts = shifts.Where(s => IsShiftOnDate(s, sourceDate)).OrderBy(s => s.StartTime).ToList();
        if (sourceShifts.Count == 0)
        {
            throw new InvalidOperationException("No shifts found on source date to copy.");
        }

        return sourceShifts;
    }

    private static bool IsShiftOnDate(ShiftEntity shift, DateOnly date)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date) == date ||
               DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date) == date;
    }

    private async Task<CommitCopyShiftsResponseDto> ExecuteCommitAsync(
        CommitCopyShiftsCommand request,
        IReadOnlyList<DateOnly> targetDates,
        IReadOnlyList<ShiftEntity> sourceShifts,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await ProcessTargetDatesAsync(request, targetDates, sourceShifts, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<CommitCopyShiftsResponseDto> ProcessTargetDatesAsync(
        CommitCopyShiftsCommand request,
        IReadOnlyList<DateOnly> targetDates,
        IReadOnlyList<ShiftEntity> sourceShifts,
        CancellationToken cancellationToken)
    {
        var copiedDates = new List<DateOnly>();
        var skippedDates = new List<DateOnly>();
        var totalReplaced = 0;

        foreach (var targetDate in targetDates)
        {
            var outcome = await ProcessSingleTargetDateAsync(request, targetDate, sourceShifts, cancellationToken);
            ApplyDateOutcome(targetDate, outcome, copiedDates, skippedDates, ref totalReplaced);
        }

        return BuildSuccessResponse(targetDates.Count, copiedDates, skippedDates, sourceShifts.Count, totalReplaced);
    }

    private async Task<TargetDateOutcome> ProcessSingleTargetDateAsync(
        CommitCopyShiftsCommand request,
        DateOnly targetDate,
        IReadOnlyList<ShiftEntity> sourceShifts,
        CancellationToken cancellationToken)
    {
        var existingShifts = await LoadTargetDateShiftsAsync(request.SiteId, targetDate, cancellationToken);
        if (existingShifts.Count == 0)
        {
            await CloneShiftsToDateAsync(request, targetDate, sourceShifts, cancellationToken);
            return TargetDateOutcome.Copied(0);
        }

        return await ResolveAndApplyConflictAsync(request, targetDate, sourceShifts, existingShifts, cancellationToken);
    }

    private async Task<List<ShiftEntity>> LoadTargetDateShiftsAsync(
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var shifts = await _shiftRepository.Query(false)
            .Where(s => s.SiteId == siteId && s.StartTime >= dayStart.AddDays(-1) && s.StartTime < dayEnd.AddDays(1))
            .ToListAsync(cancellationToken);

        return shifts.Where(s => IsShiftOnDate(s, targetDate)).ToList();
    }

    private async Task<TargetDateOutcome> ResolveAndApplyConflictAsync(
        CommitCopyShiftsCommand request,
        DateOnly targetDate,
        IReadOnlyList<ShiftEntity> sourceShifts,
        IReadOnlyList<ShiftEntity> existingShifts,
        CancellationToken cancellationToken)
    {
        if (ShouldReplaceConflictingDate(request, targetDate))
        {
            DeleteExistingShifts(existingShifts);
            await CloneShiftsToDateAsync(request, targetDate, sourceShifts, cancellationToken);
            return TargetDateOutcome.Copied(existingShifts.Count);
        }

        return TargetDateOutcome.Skipped();
    }

    private static bool ShouldReplaceConflictingDate(CommitCopyShiftsCommand request, DateOnly targetDate)
    {
        if (request.BulkReplaceAll)
        {
            return true;
        }

        return HasReplaceResolution(request.DateResolutions, targetDate);
    }

    private static bool HasReplaceResolution(
        IReadOnlyDictionary<DateOnly, CopyConflictResolution>? resolutions,
        DateOnly targetDate)
    {
        if (resolutions == null)
        {
            return false;
        }

        return resolutions.TryGetValue(targetDate, out var resolution) &&
               resolution == CopyConflictResolution.Replace;
    }

    private void DeleteExistingShifts(IReadOnlyList<ShiftEntity> shifts)
    {
        foreach (var shift in shifts)
        {
            _shiftRepository.Delete(shift);
        }
    }

    private async Task CloneShiftsToDateAsync(
        CommitCopyShiftsCommand request,
        DateOnly targetDate,
        IReadOnlyList<ShiftEntity> sourceShifts,
        CancellationToken cancellationToken)
    {
        foreach (var sourceShift in sourceShifts)
        {
            var cloned = CreateClonedShift(request, sourceShift, targetDate);
            await _shiftRepository.AddAsync(cloned, cancellationToken);
        }
    }

    private static ShiftEntity CreateClonedShift(
        CommitCopyShiftsCommand request,
        ShiftEntity source,
        DateOnly targetDate)
    {
        var duration = source.EndTime - source.StartTime;
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), source.StartTime.Offset);
        var startTime = baseDate.Add(source.StartTime.TimeOfDay);
        var endTime = startTime.Add(duration);

        return new ShiftEntity
        {
            SiteId = request.SiteId,
            JobRoleId = source.JobRoleId,
            ShiftTemplateId = source.ShiftTemplateId,
            IsPublished = source.IsPublished,
            EmployeeId = request.CopyAssignments ? source.EmployeeId : null,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private static void ApplyDateOutcome(
        DateOnly targetDate,
        TargetDateOutcome outcome,
        List<DateOnly> copiedDates,
        List<DateOnly> skippedDates,
        ref int totalReplaced)
    {
        if (outcome.IsCopied)
        {
            copiedDates.Add(targetDate);
            totalReplaced += outcome.ReplacedCount;
        }
        else
        {
            skippedDates.Add(targetDate);
        }
    }

    private static CommitCopyShiftsResponseDto BuildSuccessResponse(
        int totalProcessed,
        List<DateOnly> copiedDates,
        List<DateOnly> skippedDates,
        int shiftsPerDay,
        int totalReplaced)
    {
        var totalCreated = copiedDates.Count * shiftsPerDay;
        var message = $"Successfully copied {totalCreated} shifts across {copiedDates.Count} dates.";

        return new CommitCopyShiftsResponseDto(
            Success: true,
            TotalDatesProcessed: totalProcessed,
            CopiedDatesCount: copiedDates.Count,
            SkippedDatesCount: skippedDates.Count,
            TotalShiftsCreated: totalCreated,
            TotalShiftsReplaced: totalReplaced,
            CopiedDates: copiedDates,
            SkippedDates: skippedDates,
            Message: message
        );
    }

    private readonly record struct TargetDateOutcome(bool IsCopied, int ReplacedCount)
    {
        public static TargetDateOutcome Copied(int replacedCount) => new(true, replacedCount);
        public static TargetDateOutcome Skipped() => new(false, 0);
    }
}
