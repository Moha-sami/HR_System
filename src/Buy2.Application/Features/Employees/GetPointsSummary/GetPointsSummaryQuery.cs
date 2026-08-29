using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetPointsSummary;

public record GetPointsSummaryQuery(int EmployeeId) : IRequest<PointsSummaryDto?>;

public class GetPointsSummaryQueryHandler : IRequestHandler<GetPointsSummaryQuery, PointsSummaryDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IRepository<RewardRedemption> _rewardRedemptionRepository;

    public GetPointsSummaryQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IRepository<RewardRedemption> rewardRedemptionRepository)
    {
        _employeeRepository = employeeRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _rewardRedemptionRepository = rewardRedemptionRepository;
    }

    public async Task<PointsSummaryDto?> Handle(GetPointsSummaryQuery request, CancellationToken cancellationToken)
    {
        // 1. Check if Employee exists (and not soft-deleted)
        var employee = await _employeeRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Query PointsTransaction for EmployeeId
        var transactions = await _pointsTransactionRepository.Query()
            .Where(t => t.EmployeeId == request.EmployeeId)
            .ToListAsync(cancellationToken);

        // CurrentBalance: sum of all transaction amounts (earned positive, spent/deducted negative). Return 0 if no transactions.
        int currentBalance = transactions.Sum(t => t.Amount);

        // TotalPointsRedeemed: sum of absolute values of redemption transactions (e.g. where Type is Redemption or amount < 0 / redemption category).
        int totalPointsRedeemed = transactions
            .Where(t => t.TransactionType == TransactionType.Redeemed || t.Amount < 0)
            .Sum(t => Math.Abs(t.Amount));

        // 3. Query RewardRedemption for EmployeeId
        var redemptions = await _rewardRedemptionRepository.Query()
            .Include(r => r.RewardItem)
            .Where(r => r.EmployeeId == request.EmployeeId)
            .ToListAsync(cancellationToken);

        int totalRewardsRedeemed = redemptions.Count;
        int totalRewardsCostPoints = redemptions.Sum(r => r.RewardItem != null ? r.RewardItem.CostInPoints : 0);

        return new PointsSummaryDto(
            CurrentBalance: currentBalance,
            TotalPointsRedeemed: totalPointsRedeemed,
            TotalRewardsRedeemed: totalRewardsRedeemed,
            TotalRewardsCostPoints: totalRewardsCostPoints
        );
    }
}
