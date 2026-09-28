using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.UpdateNewsPost;

public class UpdateNewsPostCommandHandler : IRequestHandler<UpdateNewsPostCommand, NewsPostResponseDto>
{
    private readonly IRepository<Post> _postRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService? _fileStorageService;
    private readonly IValidator<UpdateNewsPostCommand> _validator;

    public UpdateNewsPostCommandHandler(
        IRepository<Post> postRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateNewsPostCommand>? validator = null,
        IFileStorageService? fileStorageService = null)
    {
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new UpdateNewsPostCommandValidator();
        _fileStorageService = fileStorageService;
    }

    public async Task<NewsPostResponseDto> Handle(
        UpdateNewsPostCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var post = await _postRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.PostType == "News", cancellationToken);

        if (post == null)
        {
            throw new KeyNotFoundException($"News post with ID {request.Id} was not found.");
        }

        // Upload replacement media if provided
        if (request.MediaFile != null && request.MediaFile.Length > 0)
        {
            if (_fileStorageService != null)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.MediaFile.FileName)}";
                post.MediaUrl = await _fileStorageService.UploadAsync(uniqueFileName, request.MediaFile);
            }
            else
            {
                post.MediaUrl = $"/storage/news/{Guid.NewGuid()}_{request.MediaFile.FileName}";
            }
        }

        var newStatus = request.Status.Trim();
        if (string.Equals(newStatus, NewsLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase) && !post.PublishedAt.HasValue)
        {
            post.PublishedAt = DateTime.UtcNow;
        }

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        post.Category = request.Category.Trim();
        post.Status = newStatus;
        post.ScheduledFor = string.Equals(newStatus, NewsLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase)
            ? request.ScheduledFor
            : null;
        post.UpdatedAt = DateTime.UtcNow;

        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new NewsPostResponseDto(
            Id: post.Id,
            Title: post.Title,
            Status: post.Status,
            Message: $"News post updated successfully with status '{post.Status}'."
        );
    }
}
