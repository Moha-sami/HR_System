using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Commands;

public record CreateRewardCommand(
    RewardCreateDto dto
) : IRequest<RewardProfileListDto>;

public class CreateRewardCommandHandler: IRequestHandler<CreateRewardCommand, RewardProfileListDto>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardCategory> _categoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    public CreateRewardCommandHandler(IRepository<RewardItem> rewardItem, IRepository<RewardCategory> category, IFileStorageService file, IUnitOfWork unitOfWork)
    {
        _rewardItemRepository = rewardItem;
        _categoryRepository = category;
        _fileStorageService = file;
        _unitOfWork = unitOfWork;
    }
    public async Task<RewardProfileListDto> Handle(CreateRewardCommand command, CancellationToken cancellation)
    {
        var categoryExit = await _categoryRepository
            .Query(false)
            .FirstOrDefaultAsync(c => c.Id == command.dto.CategoryId, cancellation);
        if (categoryExit is null)
        {
            throw new ValidationException("Category not found!");
        }

        var rewardExit = await _rewardItemRepository
            .Query(false)
            .AnyAsync(r => r.RewardName == command.dto.Name &&
                           r.CategoryId == command.dto.CategoryId &&
                           r.IsActive , cancellation);

        if (rewardExit)
        {
            throw new ValidationException("Reward item name is exists");
        }
        string? imageFile = null;
        if (command.dto.BannerImageUrl is not null)
        {
            const long maxFileSize = 1 * 1024 * 1024;

            if (command.dto.BannerImageUrl.Length == 0)
            {
                throw new ValidationException("Image file is empty.");
            }

            if (command.dto.BannerImageUrl.Length > maxFileSize)
            {
                throw new ValidationException(
                    "Image size must not exceed 1 MB.");
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(command.dto.BannerImageUrl.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ValidationException(
                    "Image extension should be one of: .jpg, .jpeg, .png.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";

            imageFile = await _fileStorageService.UploadAsync(
                fileName,
                command.dto.BannerImageUrl);
        }

        var rewardItem = new RewardItem { 
            RewardName = command.dto.Name,
            CategoryId = command.dto.CategoryId,
            BannerImageUrl = imageFile,
            Description = command.dto.Description,
            AvailableStock = 0,
            CostInPoints = command.dto.Points,
            MonetaryValue = command.dto.MonetaryValue,
            HowToRedeem = command.dto.HowToRedeem,
            TermsOfUse = command.dto.TermsOfUse,
            IsActive = true
        };

        await _rewardItemRepository.AddAsync(rewardItem, cancellation);
        await _unitOfWork.SaveChangesAsync(cancellation);

        return new RewardProfileListDto(
                rewardItem.Id,
                rewardItem.RewardName,
                rewardItem.Description,
                rewardItem.BannerImageUrl,
                rewardItem.Category.Name,
                rewardItem.CostInPoints,
                rewardItem.MonetaryValue,
                rewardItem.HowToRedeem,
                rewardItem.TermsOfUse,
                rewardItem.IsActive
            );
    }
}