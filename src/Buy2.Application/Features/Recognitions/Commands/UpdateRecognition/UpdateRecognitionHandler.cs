using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Recognitions;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Recognitions.Commands.UpdateRecognition;

public class UpdateRecognitionHandler : IRequestHandler<UpdateRecognitionCommand, RecognitionResponseDto>
{
    private readonly IRepository<Recognition> _recognitionRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateRecognitionCommand> _validator;
    private readonly IFileStorageService? _fileStorageService;

    public UpdateRecognitionHandler(
        IRepository<Recognition> recognitionRepository,
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateRecognitionCommand>? validator = null,
        IFileStorageService? fileStorageService = null)
    {
        _recognitionRepository = recognitionRepository;
        _employeeRepository = employeeRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new UpdateRecognitionValidator();
        _fileStorageService = fileStorageService;
    }

    public async Task<RecognitionResponseDto> Handle(UpdateRecognitionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Fetch recognition
        var recognition = await _recognitionRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (recognition == null)
        {
            throw new KeyNotFoundException($"Recognition with ID {request.Id} was not found.");
        }

        // 2. Authorization check: Author or elevated user (Admin/HR/Manager/SuperAdmin)
        bool isAuthor = request.ModifyingUserId.HasValue && recognition.AuthorId == request.ModifyingUserId.Value;
        bool isElevated = request.IsElevatedUser;

        if (!isAuthor && !isElevated)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this recognition.");
        }

        // 3. Verify recipient employee
        var recipient = await _employeeRepository.Query(asNoTracking: true)
            .FirstOrDefaultAsync(e => e.Id == request.RecipientId, cancellationToken);

        if (recipient == null || !recipient.IsActive || recipient.IsDeleted)
        {
            throw new KeyNotFoundException($"Active recipient employee with ID {request.RecipientId} was not found.");
        }

        // 4. Validate points grant and prevent self-recognition
        var (isValidPoints, pointsError) = RecognitionLifecycleManager.ValidatePointsGrant(request.AwardedPoints, recognition.AuthorId, recipient.Id);
        if (!isValidPoints)
        {
            throw new InvalidOperationException(pointsError);
        }

        var oldStatus = recognition.Status;
        var newStatus = request.Status?.Trim() ?? recognition.Status;
        var oldRecipientId = recognition.RecipientId;
        var newRecipientId = request.RecipientId;
        var oldPoints = recognition.AwardedPoints;
        var newPoints = request.AwardedPoints;
        var operatorId = request.ModifyingUserId ?? recognition.AuthorId;

        // 5. Points ledger adjustment
        bool wasPublished = string.Equals(oldStatus, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase);
        bool willBePublished = string.Equals(newStatus, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase);

        if (wasPublished)
        {
            if (willBePublished)
            {
                if (oldRecipientId != newRecipientId)
                {
                    // Rollback points for old recipient
                    if (oldPoints > 0)
                    {
                        var rollbackTx = RecognitionPointsGrantEngine.CreateRollbackTransaction(
                            recipientId: oldRecipientId,
                            points: oldPoints,
                            recognitionId: recognition.Id,
                            operatorId: operatorId
                        );
                        await _pointsTransactionRepository.AddAsync(rollbackTx, cancellationToken);
                    }

                    // Grant points to new recipient
                    if (newPoints > 0)
                    {
                        var grantTx = RecognitionPointsGrantEngine.CreateGrantTransaction(
                            recipientId: newRecipientId,
                            points: newPoints,
                            recognitionId: recognition.Id,
                            authorId: operatorId
                        );
                        await _pointsTransactionRepository.AddAsync(grantTx, cancellationToken);
                    }
                }
                else
                {
                    // Same recipient: adjust delta
                    var (delta, transType) = RecognitionPointsGrantEngine.CalculatePointsAdjustment(oldPoints, newPoints);
                    if (delta > 0 && transType.HasValue)
                    {
                        var pointsTx = new PointsTransaction
                        {
                            EmployeeId = newRecipientId,
                            Amount = delta,
                            TransactionType = transType.Value,
                            TriggeredBy = transType.Value == TransactionType.Add
                                ? RecognitionPointsGrantEngine.TriggeredByRecognition
                                : RecognitionPointsGrantEngine.TriggeredByRollback,
                            Comments = $"Points adjustment for recognition #{recognition.Id}: {(transType.Value == TransactionType.Add ? "+" : "-")}{delta} points",
                            CreatedByUserId = operatorId,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _pointsTransactionRepository.AddAsync(pointsTx, cancellationToken);
                    }
                }
            }
            else
            {
                // Status changed from Published to Draft/Scheduled -> retract points
                if (oldPoints > 0)
                {
                    var rollbackTx = RecognitionPointsGrantEngine.CreateRollbackTransaction(
                        recipientId: oldRecipientId,
                        points: oldPoints,
                        recognitionId: recognition.Id,
                        operatorId: operatorId
                    );
                    await _pointsTransactionRepository.AddAsync(rollbackTx, cancellationToken);
                }
            }
        }
        else
        {
            // Previously Draft or Scheduled -> now Published
            if (willBePublished && newPoints > 0)
            {
                var grantTx = RecognitionPointsGrantEngine.CreateGrantTransaction(
                    recipientId: newRecipientId,
                    points: newPoints,
                    recognitionId: recognition.Id,
                    authorId: operatorId
                );
                await _pointsTransactionRepository.AddAsync(grantTx, cancellationToken);
            }
        }

        // 6. Handle optional attachment file upload
        if (request.AttachmentFile != null && request.AttachmentFile.Length > 0)
        {
            if (_fileStorageService != null)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.AttachmentFile.FileName)}";
                recognition.AttachmentUrl = await _fileStorageService.UploadAsync(uniqueFileName, request.AttachmentFile);
            }
            else
            {
                recognition.AttachmentUrl = $"/storage/recognitions/{Guid.NewGuid()}_{request.AttachmentFile.FileName}";
            }
        }

        // 7. Mutate entity fields & audit timestamps
        recognition.RecipientId = newRecipientId;
        recognition.Title = request.Title.Trim();
        recognition.Narrative = request.Narrative.Trim();
        recognition.AwardedPoints = request.AwardedPoints;
        recognition.Badge = string.IsNullOrWhiteSpace(request.Badge) ? null : request.Badge.Trim();
        recognition.Status = newStatus;
        recognition.UpdatedAt = DateTime.UtcNow;

        if (string.Equals(newStatus, RecognitionLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase))
        {
            recognition.ScheduledFor = request.ScheduledFor;
        }
        else if (string.Equals(newStatus, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase))
        {
            if (!recognition.PublishedAt.HasValue)
            {
                recognition.PublishedAt = DateTime.UtcNow;
            }
            recognition.ScheduledFor = null;
        }
        else
        {
            recognition.ScheduledFor = null;
        }

        _recognitionRepository.Update(recognition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecognitionResponseDto(
            Id: recognition.Id,
            Title: recognition.Title,
            Status: recognition.Status,
            AwardedPoints: recognition.AwardedPoints,
            Message: "Recognition updated successfully."
        );
    }
}
