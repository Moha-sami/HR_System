using Buy2.Application.Features.ShiftTemplates;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public class UpdateShiftTemplateBlockDtoValidator : AbstractValidator<UpdateShiftTemplateBlockDto>
{
    public UpdateShiftTemplateBlockDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Block Id must be greater than or equal to 0. Use 0 for new blocks.");

        RuleFor(x => x.StartTime)
            .Must(BeParseableTime)
            .WithMessage("Block StartTime must be a valid time in 'hh:mm tt' format (e.g. '09:00 AM').");

        RuleFor(x => x.EndTime)
            .Must(BeParseableTime)
            .WithMessage("Block EndTime must be a valid time in 'hh:mm tt' format (e.g. '05:00 PM').");

        RuleFor(x => x)
            .Must(HaveDifferentTimes)
            .WithMessage("Block StartTime and EndTime must be different.");

        RuleFor(x => x.JobRoleId)
            .GreaterThan(0)
            .WithMessage("JobRoleId must be greater than 0.");

        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0)
            .WithMessage("AssignedUserId must be greater than 0.");
    }

    private static bool BeParseableTime(string? value)
    {
        return ShiftTimeHelper.TryParseTime(value, out _);
    }

    private static bool HaveDifferentTimes(UpdateShiftTemplateBlockDto block)
    {
        if (!ShiftTimeHelper.TryParseTime(block.StartTime, out var start)
            || !ShiftTimeHelper.TryParseTime(block.EndTime, out var end))
        {
            return true;
        }

        return start != end;
    }
}
