using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Validators.Rewards;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Commands;

public record CreateRewardCommand(
    RewardCreateDto Dto,
    IFormFile? ImageFile
) : IRequest<Result<RewardProfileListDto>>;

public class CreateRewardCommandHandler : IRequestHandler<CreateRewardCommand, Result<RewardProfileListDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardCategory> _categoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRewardCommandHandler(
        IRepository<RewardItem> rewardItem,
        IRepository<RewardCategory> category,
        IFileStorageService file,
        IUnitOfWork unitOfWork)
    {
        _rewardItemRepository = rewardItem;
        _categoryRepository = category;
        _fileStorageService = file;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RewardProfileListDto>> Handle(CreateRewardCommand command, CancellationToken cancellation)
    {
        if (command.Dto is null)
        {
            return Result<RewardProfileListDto>.ValidationFailure("Reward data is required.");
        }

        var validationResult = await new RewardCreateDtoValidator().ValidateAsync(command.Dto, cancellation);
        if (!validationResult.IsValid)
        {
            return Result<RewardProfileListDto>.ValidationFailure(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var category = await _categoryRepository
            .Query(false)
            .FirstOrDefaultAsync(c => c.Id == command.Dto.CategoryId, cancellation);

        if (category is null)
        {
            return Result<RewardProfileListDto>.NotFound("Category not found!");
        }

        var rewardExists = await _rewardItemRepository
            .Query(false)
            .AnyAsync(r => r.RewardName == command.Dto.Name &&
                           r.CategoryId == command.Dto.CategoryId &&
                           r.IsActive, cancellation);

        if (rewardExists)
        {
            return Result<RewardProfileListDto>.Conflict("Reward item name already exists in this category.");
        }

        string? imageFile = command.Dto.BannerImageUrl;
        if (command.ImageFile is not null)
        {
            const long maxFileSize = 1 * 1024 * 1024;

            if (command.ImageFile.Length == 0)
            {
                return Result<RewardProfileListDto>.ValidationFailure("Image file is empty.");
            }

            if (command.ImageFile.Length > maxFileSize)
            {
                return Result<RewardProfileListDto>.ValidationFailure("Image size must not exceed 1 MB.");
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(command.ImageFile.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return Result<RewardProfileListDto>.ValidationFailure(
                    "Image extension should be one of: .jpg, .jpeg, .png.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";

            imageFile = await _fileStorageService.UploadAsync(
                fileName,
                command.ImageFile);
        }

        var rewardItem = new RewardItem
        {
            RewardName = command.Dto.Name,
            CategoryId = command.Dto.CategoryId,
            BannerImageUrl = imageFile,
            Description = command.Dto.Description,
            AvailableStock = 0,
            CostInPoints = command.Dto.Points,
            MonetaryValue = command.Dto.MonetaryValue,
            HowToRedeem = command.Dto.HowToRedeem,
            TermsOfUse = command.Dto.TermsOfUse,
            IsActive = true
        };

        await _rewardItemRepository.AddAsync(rewardItem, cancellation);
        await _unitOfWork.SaveChangesAsync(cancellation);

        var resultDto = new RewardProfileListDto(
            rewardItem.Id,
            rewardItem.RewardName,
            rewardItem.Description,
            rewardItem.BannerImageUrl,
            category.Name,
            rewardItem.CostInPoints,
            rewardItem.MonetaryValue,
            rewardItem.HowToRedeem,
            rewardItem.TermsOfUse,
            rewardItem.IsActive
        );

        return Result<RewardProfileListDto>.Success(resultDto);
    }
}