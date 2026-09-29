using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Recognitions.Commands.DeleteRecognition;

public class DeleteRecognitionHandler : IRequestHandler<DeleteRecognitionCommand, bool>
{
    private readonly IRepository<Recognition> _recognitionRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRecognitionHandler(
        IRepository<Recognition> recognitionRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _recognitionRepository = recognitionRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRecognitionCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify recognition exists and is not already deleted
        var recognition = await _recognitionRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (recognition == null)
        {
            throw new KeyNotFoundException($"Recognition with ID {request.Id} was not found.");
        }

        // 2. Verify administrative authority (Admin/HR/Manager/SuperAdmin or original author)
        bool isAuthor = request.CurrentUserId.HasValue && recognition.AuthorId == request.CurrentUserId.Value;
        bool isElevated = request.IsElevatedUser;

        if (!isElevated && !isAuthor)
        {
            throw new UnauthorizedAccessException("You do not have administrative authority to delete this recognition.");
        }

        // 3. Points reversal safeguard: If published with points, credit a rollback transaction
        bool wasPublished = string.Equals(recognition.Status, RecognitionLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase);

        if (wasPublished && recognition.AwardedPoints > 0)
        {
            var operatorId = request.CurrentUserId ?? recognition.AuthorId;
            var rollbackTx = RecognitionPointsGrantEngine.CreateRollbackTransaction(
                recipientId: recognition.RecipientId,
                points: recognition.AwardedPoints,
                recognitionId: recognition.Id,
                operatorId: operatorId
            );

            await _pointsTransactionRepository.AddAsync(rollbackTx, cancellationToken);
        }

        // 4. Soft-delete recognition entity
        recognition.IsDeleted = true;
        recognition.DeletedAt = DateTime.UtcNow;

        _recognitionRepository.Update(recognition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
