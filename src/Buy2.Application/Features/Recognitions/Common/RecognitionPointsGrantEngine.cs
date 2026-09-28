using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using System;

namespace Buy2.Application.Features.Recognitions.Common;

public static class RecognitionPointsGrantEngine
{
    public const string TriggeredByRecognition = "RecognitionAward";
    public const string TriggeredByRollback = "RecognitionRollback";

    public static PointsTransaction CreateGrantTransaction(
        int recipientId,
        int points,
        int recognitionId,
        int authorId)
    {
        return new PointsTransaction
        {
            EmployeeId = recipientId,
            Amount = points,
            TransactionType = TransactionType.Add,
            TriggeredBy = TriggeredByRecognition,
            Comments = $"Recognition award points for recognition #{recognitionId}",
            CreatedByUserId = authorId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static PointsTransaction CreateRollbackTransaction(
        int recipientId,
        int points,
        int recognitionId,
        int operatorId)
    {
        return new PointsTransaction
        {
            EmployeeId = recipientId,
            Amount = points,
            TransactionType = TransactionType.Deduct,
            TriggeredBy = TriggeredByRollback,
            Comments = $"Safeguard reversal of recognition award points for retracted recognition #{recognitionId}",
            CreatedByUserId = operatorId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static (int Delta, TransactionType? TransactionType) CalculatePointsAdjustment(int oldPoints, int newPoints)
    {
        var delta = newPoints - oldPoints;
        if (delta > 0)
        {
            return (delta, TransactionType.Add);
        }
        else if (delta < 0)
        {
            return (Math.Abs(delta), TransactionType.Deduct);
        }

        return (0, null);
    }
}
