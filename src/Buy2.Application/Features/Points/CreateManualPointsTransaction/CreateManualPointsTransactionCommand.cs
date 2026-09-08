using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Points.CreateManualPointsTransaction;

public record CreateManualPointsTransactionCommand(
    int EmployeeId,
    string TransactionType,
    decimal PointsValue,
    string Comments
) : IRequest<CreateManualPointsTransactionResult>;

public record CreateManualPointsTransactionResult(
    bool IsSuccess,
    int? TransactionId = null,
    string? ErrorMessage = null,
    bool IsNotFound = false);

public class CreateManualPointsTransactionCommandHandler : IRequestHandler<CreateManualPointsTransactionCommand, CreateManualPointsTransactionResult>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateManualPointsTransactionCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateManualPointsTransactionResult> Handle(CreateManualPointsTransactionCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return new CreateManualPointsTransactionResult(
                IsSuccess: false,
                ErrorMessage: "Employee not found.",
                IsNotFound: true);
        }

        if (!Enum.TryParse<TransactionType>(request.TransactionType, true, out var transactionType))
        {
            return new CreateManualPointsTransactionResult(
                IsSuccess: false,
                ErrorMessage: "Invalid TransactionType. Must be Add or Deduct.");
        }

        if (transactionType != TransactionType.Add && transactionType != TransactionType.Deduct)
        {
            return new CreateManualPointsTransactionResult(
                IsSuccess: false,
                ErrorMessage: "TransactionType must be Add or Deduct for manual adjustments.");
        }

        var amount = transactionType == TransactionType.Add
            ? (int)request.PointsValue
            : -(int)request.PointsValue;

        var transaction = new PointsTransaction
        {
            EmployeeId = request.EmployeeId,
            Amount = amount,
            TransactionType = transactionType,
            TriggeredBy = "ManualAdjustment",
            Comments = request.Comments,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _pointsTransactionRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateManualPointsTransactionResult(
            IsSuccess: true,
            TransactionId: transaction.Id);
    }
}