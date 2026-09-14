using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Commands;

public record UpdateRewardCommand(
    int Id, RewardUpdateDto dto, IFormFile? ImageFile
) : IRequest<RewardProfileListDto>;

public class UpdateRewardCommandHandler : IRequestHandler<UpdateRewardCommand, RewardProfileListDto>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardCategory> _rewardCategoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateRewardCommandHandler(IRepository<RewardItem> rewardItem, IRepository<RewardCategory> rewardCategory, IFileStorageService file, IUnitOfWork unit)
    {
        _rewardItemRepository = rewardItem;
        _rewardCategoryRepository = rewardCategory;
        _fileStorageService = file;
        _unitOfWork = unit;
    }
    public async Task<RewardProfileListDto> Handle(UpdateRewardCommand command, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
          .Query()
          .Include(r => r.Category)
          .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);
        if (rewardItem is null)
        {
            throw new ValidationException("Reward item not found.");
        }

        var categoryExit = await _rewardCategoryRepository
            .Query(false)
            .AnyAsync(c => c.Id == command.dto.CategoryId, cancellation);

        if (!categoryExit)
        {
            throw new ValidationException("Category not found.");
        }

        var rewardExit = await _rewardItemRepository
            .Query(false)
            .AnyAsync(r =>
            r.Id != command.Id &&
            r.CategoryId == command.dto.CategoryId &&
            r.RewardName == command.dto.Name &&
            r.IsActive
            , cancellation);

        if (rewardExit)
        {
            throw new ValidationException("Reward item category not exist.");
        }

        string? imageFile = command.dto.BannerImageUrl;
        if (imageFile is not null)
        {
            if (command.ImageFile == null || command.ImageFile.Length == 0)
            {
                throw new ValidationException("Image not found.");
            }

            long max = 1024 * 1024 * 1;
            if (command.ImageFile.Length > max)
            {
                throw new ValidationException("Image size too much.");
            }

            var extension = Path.GetExtension(command.ImageFile.FileName).ToLowerInvariant();
            string[] allowedExtension = [".jpg", ".jpeg", ".png"];
            if (!allowedExtension.Contains(extension))
            {
                throw new ValidationException("Image extension should be one of: .jpg, .jpeg, .png .");
            }

            string fileName = $"{Guid.NewGuid}{extension}";
            imageFile = await _fileStorageService.UploadAsync(fileName, command.ImageFile);
        }

        rewardItem.RewardName = command.dto.Name;
        rewardItem.Description = command.dto.Description;
        rewardItem.BannerImageUrl = command.dto.BannerImageUrl;
        rewardItem.CategoryId = command.dto.CategoryId;
        rewardItem.CostInPoints = command.dto.Points;
        rewardItem.MonetaryValue = command.dto.MonetaryValue;
        rewardItem.HowToRedeem = command.dto.HowToRedeem;
        rewardItem.TermsOfUse = command.dto.TermsOfUse;
        rewardItem.IsActive = true;

        await _unitOfWork.SaveChangesAsync(cancellation);

        return new RewardProfileListDto(
               command.Id,
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