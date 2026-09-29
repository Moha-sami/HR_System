using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Recognitions;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Recognitions.Commands.CreateRecognition;

public class CreateRecognitionHandler : IRequestHandler<CreateRecognitionCommand, RecognitionResponseDto>
{
    private readonly IRepository<Recognition> _recognitionRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService? _fileStorageService;
    private readonly IValidator<CreateRecognitionCommand> _validator;

    public CreateRecognitionHandler(
        IRepository<Recognition> recognitionRepository,
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateRecognitionCommand>? validator = null,
        IFileStorageService? fileStorageService = null)
    {
        _recognitionRepository = recognitionRepository;
        _employeeRepository = employeeRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new CreateRecognitionValidator();
        _fileStorageService = fileStorageService;
    }

    public async Task<RecognitionResponseDto> Handle(
        CreateRecognitionCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Verify recipient employee exists and is active
        var recipient = await _employeeRepository.Query(asNoTracking: true)
            .FirstOrDefaultAsync(e => e.Id == request.RecipientId, cancellationToken);

        if (recipient == null || !recipient.IsActive || recipient.IsDeleted)
        {
            throw new KeyNotFoundException($"Active recipient employee with ID {request.RecipientId} was not found.");
        }

        // 2. Resolve author employee
        int authorId;
        if (request.AuthorId.HasValue && request.AuthorId.Value > 0)
        {
            var author = await _employeeRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(e => e.Id == request.AuthorId.Value, cancellationToken);

            if (author == null)
            {
                throw new KeyNotFoundException($"Author employee with ID {request.AuthorId.Value} was not found.");
            }
            authorId = author.Id;
        }
        else
        {
            var defaultAuthor = await _employeeRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultAuthor == null)
            {
                throw new InvalidOperationException("No employee profile found to author recognition.");
            }
            authorId = defaultAuthor.Id;
        }

        // 3. Validate points grant and prevent self-recognition
        var (isValidPoints, pointsError) = RecognitionLifecycleManager.ValidatePointsGrant(request.AwardedPoints, authorId, recipient.Id);
        if (!isValidPoints)
        {
            throw new InvalidOperationException(pointsError);
        }

        // 4. Handle attachment file upload
        string? attachmentUrl = null;
        if (request.AttachmentFile != null && request.AttachmentFile.Length > 0)
        {
            if (_fileStorageService != null)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.AttachmentFile.FileName)}";
                attachmentUrl = await _fileStorageService.UploadAsync(uniqueFileName, request.AttachmentFile);
            }
            else
            {
                attachmentUrl = $"/storage/recognitions/{Guid.NewGuid()}_{request.AttachmentFile.FileName}";
            }
        }

        // 5. Determine publish and schedule timestamps
        var status = request.Status?.Trim() ?? RecognitionLifecycleManager.StatusPublished;
        DateTime? publishedAt = null;
        DateTime? scheduledFor = null;

        if (string.Equals(status, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase))
        {
            publishedAt = DateTime.UtcNow;
        }
        else if (string.Equals(status, RecognitionLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase))
        {
            scheduledFor = request.ScheduledFor;
        }

        // 6. Stage recognition entity
        var recognition = new Recognition
        {
            AuthorId = authorId,
            RecipientId = recipient.Id,
            Title = request.Title.Trim(),
            Narrative = request.Narrative.Trim(),
            Badge = string.IsNullOrWhiteSpace(request.Badge) ? null : request.Badge.Trim(),
            AwardedPoints = request.AwardedPoints,
            Status = status,
            ScheduledFor = scheduledFor,
            PublishedAt = publishedAt,
            AttachmentUrl = attachmentUrl,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _recognitionRepository.AddAsync(recognition, cancellationToken);

        // 7. Dispatch points allocation if published immediately
        if (string.Equals(status, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase) && request.AwardedPoints > 0)
        {
            var pointsTx = RecognitionPointsGrantEngine.CreateGrantTransaction(
                recipientId: recipient.Id,
                points: request.AwardedPoints,
                recognitionId: recognition.Id,
                authorId: authorId
            );

            await _pointsTransactionRepository.AddAsync(pointsTx, cancellationToken);
        }

        // 8. Commit transaction atomically via Unit of Work
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecognitionResponseDto(
            Id: recognition.Id,
            Title: recognition.Title,
            Status: recognition.Status,
            AwardedPoints: recognition.AwardedPoints,
            Message: $"Recognition created successfully with status '{recognition.Status}'."
        );
    }
}
