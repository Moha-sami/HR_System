using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Validators.Rewards;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Commands;

public record UpdateRewardCommand(
    int Id,
    RewardUpdateDto Dto,
    IFormFile? ImageFile
) : IRequest<Result<RewardProfileListDto>>;

public class UpdateRewardCommandHandler : IRequestHandler<UpdateRewardCommand, Result<RewardProfileListDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardCategory> _rewardCategoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRewardCommandHandler(
        IRepository<RewardItem> rewardItemRepository,
        IRepository<RewardCategory> rewardCategoryRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _rewardItemRepository = rewardItemRepository;
        _rewardCategoryRepository = rewardCategoryRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RewardProfileListDto>> Handle(UpdateRewardCommand command, CancellationToken cancellation)
    {
        if (command.Dto is null)
        {
            return Result<RewardProfileListDto>.ValidationFailure("Reward data is required.");
        }

        var validationResult = await new RewardUpdateDtoValidator().ValidateAsync(command.Dto, cancellation);
        if (!validationResult.IsValid)
        {
            return Result<RewardProfileListDto>.ValidationFailure(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var rewardItem = await _rewardItemRepository
            .Query(false)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);

        if (rewardItem is null)
        {
            return Result<RewardProfileListDto>.NotFound("Reward item not found.");
        }

        var category = await _rewardCategoryRepository
            .Query(false)
            .FirstOrDefaultAsync(c => c.Id == command.Dto.CategoryId, cancellation);

        if (category is null)
        {
            return Result<RewardProfileListDto>.NotFound("Category not found.");
        }

        var rewardExists = await _rewardItemRepository
            .Query(false)
            .AnyAsync(r =>
                r.Id != command.Id &&
                r.CategoryId == command.Dto.CategoryId &&
                r.RewardName == command.Dto.Name &&
                r.IsActive,
                cancellation);

        if (rewardExists)
        {
            return Result<RewardProfileListDto>.Conflict("Reward item name already exists in this category.");
        }

        string? bannerImageUrl = command.Dto.BannerImageUrl ?? rewardItem.BannerImageUrl;

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

            bannerImageUrl = await _fileStorageService.UploadAsync(
                fileName,
                command.ImageFile);
        }

        rewardItem.RewardName = command.Dto.Name;
        rewardItem.Description = command.Dto.Description;
        rewardItem.BannerImageUrl = bannerImageUrl;
        rewardItem.CategoryId = command.Dto.CategoryId;
        rewardItem.Category = category;
        rewardItem.CostInPoints = command.Dto.Points;
        rewardItem.MonetaryValue = command.Dto.MonetaryValue;
        rewardItem.HowToRedeem = command.Dto.HowToRedeem;
        rewardItem.TermsOfUse = command.Dto.TermsOfUse;

        _rewardItemRepository.Update(rewardItem);
        await _unitOfWork.SaveChangesAsync(cancellation);

        var resultDto = new RewardProfileListDto(
            command.Id,
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