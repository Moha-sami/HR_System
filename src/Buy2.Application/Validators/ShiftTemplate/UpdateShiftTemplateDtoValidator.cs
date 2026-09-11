using Buy2.Application.Features.ShiftTemplates.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public class UpdateShiftTemplateDtoValidator : AbstractValidator<UpdateShiftTemplateDto>
{
    public UpdateShiftTemplateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.StartTime)
            .Must(BeParseableTime)
            .WithMessage("StartTime must be a valid time in 'hh:mm tt' format (e.g. '09:00 AM').");

        RuleFor(x => x.EndTime)
            .Must(BeParseableTime)
            .WithMessage("EndTime must be a valid time in 'hh:mm tt' format (e.g. '05:00 PM').");

        RuleFor(x => x)
            .Must(HaveDifferentTimes)
            .When(IsValidTemplateTimes)
            .WithMessage("StartTime and EndTime must be different.");

        RuleFor(x => x.SiteIds)
            .NotNull()
            .WithMessage("At least one SiteId is required.")
            .NotEmpty()
            .WithMessage("At least one SiteId is required.");

        RuleForEach(x => x.SiteIds)
            .GreaterThan(0)
            .WithMessage("SiteIds must be greater than 0.");

        RuleFor(x => x.SiteIds)
            .Must(BeDistinct)
            .When(x => x.SiteIds is not null)
            .WithMessage("SiteIds must be distinct.");

        RuleFor(x => x.ShiftBlocks)
            .NotNull()
            .WithMessage("At least one shift block is required.")
            .NotEmpty()
            .WithMessage("At least one shift block is required.");

        RuleForEach(x => x.ShiftBlocks)
            .SetValidator(new UpdateShiftTemplateBlockDtoValidator())
            .When(x => x.ShiftBlocks is not null);

        RuleFor(x => x.ShiftBlocks)
            .Must(HaveDistinctBlockIds)
            .When(x => x.ShiftBlocks is not null)
            .WithMessage("Shift block Ids must be distinct.");

        RuleForEach(x => x.ShiftBlocks)
            .Must((dto, block) => IsBlockWithinTemplate(dto, block.StartTime, block.EndTime))
            .When(x => x.ShiftBlocks is not null && IsValidTemplateTimes(x))
            .WithMessage((dto, block) => $"Block {GetBlockPosition(dto.ShiftBlocks, block)} is outside the template range.");

        RuleFor(x => x)
            .Must(HaveNoOverlappingBlocks)
            .When(x => x.ShiftBlocks is not null && IsValidTemplateTimes(x))
            .WithMessage("Shift blocks for the same employee must not overlap.");
    }

    private static bool BeParseableTime(string? value)
    {
        return ShiftTimeHelper.TryParseTime(value, out _);
    }

    private static bool HaveDifferentTimes(UpdateShiftTemplateDto dto)
    {
        // Guarded by When(IsValidTemplateTimes): both values are parseable here.
        ShiftTimeHelper.TryParseTime(dto.StartTime, out var start);
        ShiftTimeHelper.TryParseTime(dto.EndTime, out var end);

        return start != end;
    }

    private static bool BeDistinct(List<int>? siteIds)
    {
        if (siteIds is null)
        {
            return true;
        }

        return siteIds.Distinct().Count() == siteIds.Count;
    }

    private static bool HaveDistinctBlockIds(List<UpdateShiftTemplateBlockDto>? blocks)
    {
        if (blocks is null)
        {
            return true;
        }

        var existingIds = blocks
            .Where(b => b.Id > 0)
            .Select(b => b.Id)
            .ToList();

        return existingIds.Distinct().Count() == existingIds.Count;
    }

    private static bool IsBlockWithinTemplate(UpdateShiftTemplateDto dto, string? blockStartTime, string? blockEndTime)
    {
        // Layered ownership: template-level failures belong to the StartTime/EndTime
        // rules (see When guard), and unreadable block times belong to the block
        // validator — they must never be misreported as "outside the template range".
        // Night shifts are handled by IsWithinTemplate via template-relative offsets
        // (no naive blockStart >= templateStart && blockEnd <= templateEnd comparison).
        if (!ShiftTimeHelper.TryParseTime(dto.StartTime, out var templateStart)
            || !ShiftTimeHelper.TryParseTime(dto.EndTime, out var templateEnd))
        {
            return true;
        }

        if (!ShiftTimeHelper.TryParseTime(blockStartTime, out var blockStart)
            || !ShiftTimeHelper.TryParseTime(blockEndTime, out var blockEnd))
        {
            return true;
        }

        return ShiftTimeHelper.IsWithinTemplate(blockStart, blockEnd, templateStart, templateEnd);
    }

    private static int GetBlockPosition(List<UpdateShiftTemplateBlockDto>? blocks, UpdateShiftTemplateBlockDto block)
    {
        if (blocks is null)
        {
            return 0;
        }

        return blocks.IndexOf(block) + 1;
    }

    private static bool IsValidTemplateTimes(UpdateShiftTemplateDto dto)
    {
        return ShiftTimeHelper.TryParseTime(dto.StartTime, out _)
            && ShiftTimeHelper.TryParseTime(dto.EndTime, out _);
    }

    private static bool HaveNoOverlappingBlocks(UpdateShiftTemplateDto dto)
    {
        // Only speaks about overlap. Invalid template times are reported by the
        // StartTime/EndTime rules; this rule never runs for them (see When guard above).
        ShiftTimeHelper.TryParseTime(dto.StartTime, out var templateStart);
        ShiftTimeHelper.TryParseTime(dto.EndTime, out var templateEnd);

        var intervals = new List<(TimeSpan Start, TimeSpan End, int EmployeeId)>();
        foreach (var block in dto.ShiftBlocks ?? Enumerable.Empty<UpdateShiftTemplateBlockDto>())
        {
            if (!ShiftTimeHelper.TryParseTime(block.StartTime, out var blockStart)
                || !ShiftTimeHelper.TryParseTime(block.EndTime, out var blockEnd))
            {
                continue;
            }

            if (!ShiftTimeHelper.IsWithinTemplate(blockStart, blockEnd, templateStart, templateEnd))
            {
                continue;
            }

            intervals.Add((blockStart, blockEnd, block.AssignedUserId));
        }

        return ShiftTimeHelper.GetOverlappingBlocksForSameEmployee(intervals, templateStart).Count == 0;
    }
}
