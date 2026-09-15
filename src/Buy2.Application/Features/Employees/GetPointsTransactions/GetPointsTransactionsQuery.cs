using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;

namespace Buy2.Application.Features.Employees.GetPointsTransactions;

public record GetPointsTransactionsQuery(
    int EmployeeId,
    int Page = 1,
    int PageSize = 10,
    TransactionType? Type = null,
    string? TriggeredBy = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null
) : IRequest<PaginatedPointsTransactionsDto?>;

public class GetPointsTransactionsQueryHandler : IRequestHandler<GetPointsTransactionsQuery, PaginatedPointsTransactionsDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;

    public GetPointsTransactionsQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsTransactionRepository)
    {
        _employeeRepository = employeeRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
    }

    public async Task<PaginatedPointsTransactionsDto?> Handle(GetPointsTransactionsQuery request, CancellationToken cancellationToken)
    {
        // 1. Check if Employee exists (and is not soft-deleted)
        var employee = await _employeeRepository.FirstOrDefaultAsync(
            e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Validate and clamp pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // 3. Base specification for employee points transactions with eager loaded PointsRule
        var spec = new Specification<PointsTransaction>()
            .Include(nameof(PointsTransaction.PointsRule))
            .Where(t => t.EmployeeId == request.EmployeeId);

        // 4. Filter by Type
        if (request.Type.HasValue)
        {
            var type = request.Type.Value;
            if (type == TransactionType.Earned || type == TransactionType.Add)
            {
                spec.Where(t => t.Amount > 0);
            }
            else if (type == TransactionType.Redeemed || type == TransactionType.Deduct)
            {
                spec.Where(t => t.Amount < 0);
            }
            else
            {
                spec.Where(t => t.TransactionType == type);
            }
        }

        // 5. Filter by TriggeredBy / Reason
        if (!string.IsNullOrWhiteSpace(request.TriggeredBy))
        {
            var triggeredBy = request.TriggeredBy.Trim();
            spec.Where(t =>
                (t.PointsRule != null && (t.PointsRule.RuleKey.Contains(triggeredBy) || t.PointsRule.EventType.Contains(triggeredBy))) ||
                t.TriggeredBy.Contains(triggeredBy));
        }

        // 6. Filter by Date range
        if (request.DateFrom.HasValue)
        {
            spec.Where(t => t.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            spec.Where(t => t.CreatedAt <= request.DateTo.Value);
        }

        // 7-8. Order by CreatedAt descending and materialize paginated records
        spec.OrderBy(t => t.CreatedAt, descending: true);
        var pagedResult = await _pointsTransactionRepository.PagedAsync(spec, page, pageSize, cancellationToken);
        var totalCount = pagedResult.TotalCount;
        var pagedRecords = pagedResult.Items;

        // 9. Map items cleanly into DTOs
        var items = pagedRecords.Select(MapToDto).ToList();

        // 10. Calculate total pages
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedPointsTransactionsDto(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        );
    }

    private static PointsTransactionDto MapToDto(PointsTransaction t)
    {
        var transactionType = t.TransactionType.ToString();

        var triggeredBy = t.TriggeredBy;
        var comments = t.Comments;

        return new PointsTransactionDto(
            Id: t.Id,
            Date: t.CreatedAt,
            Amount: t.Amount,
            Type: transactionType,
            TriggeredBy: triggeredBy,
            Comments: comments
        );
    }
}
