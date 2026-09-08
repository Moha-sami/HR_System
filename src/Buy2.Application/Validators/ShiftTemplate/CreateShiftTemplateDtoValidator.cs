using Buy2.Application.Features.ShiftTemplates.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public class CreateShiftTemplateDtoValidator : AbstractValidator<CreateShiftTemplateDto>
{
    public CreateShiftTemplateDtoValidator()
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
            .SetValidator(new CreateShiftTemplateBlockDtoValidator())
            .When(x => x.ShiftBlocks is not null);

        RuleFor(x => x)
            .Must(AllBlocksWithinTemplate)
            .When(x => x.ShiftBlocks is not null)
            .WithMessage("One or more shift blocks must be within template time.");
    }

    private static bool BeParseableTime(string? value)
    {
        return ShiftTimeHelper.TryParseTime(value, out _);
    }

    private static bool HaveDifferentTimes(CreateShiftTemplateDto dto)
    {
        if (!ShiftTimeHelper.TryParseTime(dto.StartTime, out var start)
            || !ShiftTimeHelper.TryParseTime(dto.EndTime, out var end))
        {
            return true;
        }

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

    private static bool AllBlocksWithinTemplate(CreateShiftTemplateDto dto)
    {
        if (!ShiftTimeHelper.TryParseTime(dto.StartTime, out var templateStart)
            || !ShiftTimeHelper.TryParseTime(dto.EndTime, out var templateEnd))
        {
            return true;
        }

        foreach (var block in dto.ShiftBlocks ?? Enumerable.Empty<CreateShiftTemplateBlockDto>())
        {
            if (!ShiftTimeHelper.TryParseTime(block.StartTime, out var blockStart)
                || !ShiftTimeHelper.TryParseTime(block.EndTime, out var blockEnd))
            {
                continue;
            }

            if (!ShiftTimeHelper.IsWithinTemplate(blockStart, blockEnd, templateStart, templateEnd))
            {
                return false;
            }
        }

        return true;
    }
}
