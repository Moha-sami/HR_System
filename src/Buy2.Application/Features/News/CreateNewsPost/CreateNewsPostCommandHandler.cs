using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.CreateNewsPost;

public class CreateNewsPostCommandHandler : IRequestHandler<CreateNewsPostCommand, NewsPostResponseDto>
{
    private readonly IRepository<Post> _postRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService? _fileStorageService;
    private readonly IValidator<CreateNewsPostCommand> _validator;

    public CreateNewsPostCommandHandler(
        IRepository<Post> postRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateNewsPostCommand>? validator = null,
        IFileStorageService? fileStorageService = null)
    {
        _postRepository = postRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new CreateNewsPostCommandValidator();
        _fileStorageService = fileStorageService;
    }

    public async Task<NewsPostResponseDto> Handle(
        CreateNewsPostCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Resolve author
        int authorId;
        if (request.AuthorId.HasValue)
        {
            var author = await _employeeRepository.GetByIdAsync(request.AuthorId.Value, cancellationToken);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with ID {request.AuthorId.Value} does not exist.");
            }
            authorId = author.Id;
        }
        else
        {
            var defaultAuthor = await _employeeRepository.Query(asNoTracking: true).FirstOrDefaultAsync(cancellationToken);
            if (defaultAuthor == null)
            {
                throw new InvalidOperationException("No employee profile found to assign as post author.");
            }
            authorId = defaultAuthor.Id;
        }

        // 2. Upload media file if provided
        string? mediaUrl = null;
        if (request.MediaFile != null && request.MediaFile.Length > 0)
        {
            if (_fileStorageService != null)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.MediaFile.FileName)}";
                mediaUrl = await _fileStorageService.UploadAsync(uniqueFileName, request.MediaFile);
            }
            else
            {
                mediaUrl = $"/storage/news/{Guid.NewGuid()}_{request.MediaFile.FileName}";
            }
        }

        // 3. Determine publish and schedule timestamps
        var status = request.Status?.Trim() ?? NewsLifecycleManager.StatusDraft;
        DateTime? publishedAt = null;
        DateTime? scheduledFor = null;

        if (string.Equals(status, NewsLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase))
        {
            publishedAt = DateTime.UtcNow;
        }
        else if (string.Equals(status, NewsLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase))
        {
            scheduledFor = request.ScheduledFor;
        }

        // 4. Create and persist Post entity
        var post = new Post
        {
            AuthorId = authorId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Category = request.Category.Trim(),
            Status = status,
            ScheduledFor = scheduledFor,
            PublishedAt = publishedAt,
            MediaUrl = mediaUrl,
            PostType = "News",
            LikesCount = 0,
            CommentsCount = 0,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepository.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new NewsPostResponseDto(
            Id: post.Id,
            Title: post.Title,
            Status: post.Status,
            Message: $"News post created successfully with status '{post.Status}'."
        );
    }
}
