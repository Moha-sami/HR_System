using Buy2.Application.DTOs.Rewards.DTOs;
using FluentValidation;
using System.Runtime.CompilerServices;

namespace Buy2.Application.Validators.Rewards;

public class BulkVoucherUploadDtoValidator : AbstractValidator<RewardBulkExcelUpload>
{
    private static readonly string[] _AllowExtension = { ".xlsx", ".xls", ".csv" };

    public BulkVoucherUploadDtoValidator()
    {
        RuleFor(r => r.RewardItemId)
            .NotEmpty()
            .GreaterThan(0);
        RuleFor(r => r.File)
            .Must(file =>
            {
                if (file == null)
                    return false;

                var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

                return _AllowExtension.Contains(extension);
            }).WithMessage("Only .xlsx, .xls, and .csv files are allowed.");
    }
}