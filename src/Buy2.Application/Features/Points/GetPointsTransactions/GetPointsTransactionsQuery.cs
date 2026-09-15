using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;

namespace Buy2.Application.Features.Points.GetPointsTransactions;

public record GetPointsTransactionsQuery(
    PointsTransactionFilterQueryDto Filter
) : IRequest<PaginatedPointsTransactionsResponseDto>;

public class GetPointsTransactionsQueryHandler : IRequestHandler<GetPointsTransactionsQuery, PaginatedPointsTransactionsResponseDto>
{
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetPointsTransactionsQueryHandler(
        IRepository<PointsTransaction> pointsTransactionRepository,
        IRepository<Employee> employeeRepository)
    {
        _pointsTransactionRepository = pointsTransactionRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<PaginatedPointsTransactionsResponseDto> Handle(GetPointsTransactionsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var spec = new Specification<PointsTransaction>()
            .Include("Employee", "Employee.JobRole", "Employee.JobRole.Department", "Employee.Site", "PointsRule")
            .Where(t => !t.Employee.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim().ToLower();
            spec.Where(t =>
                t.Employee.FirstName.ToLower().Contains(searchTerm) ||
                t.Employee.LastName.ToLower().Contains(searchTerm) ||
                t.Employee.EmployeeCode.ToLower().Contains(searchTerm));
        }

        if (filter.Month.HasValue)
        {
            var month = filter.Month.Value;
            if (month < 1 || month > 12)
            {
                throw new ArgumentException("Month must be between 1 and 12.");
            }

            var year = filter.Year ?? DateTimeOffset.UtcNow.Year;
            var startDate = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            spec.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);
        }
        else
        {
            if (filter.DateFrom.HasValue)
            {
                var dateFrom = filter.DateFrom.Value;
                spec.Where(t => t.CreatedAt >= dateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dateTo = filter.DateTo.Value;
                spec.Where(t => t.CreatedAt <= dateTo);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType))
        {
            var type = filter.TransactionType.Trim();
            if (Enum.TryParse<TransactionType>(type, true, out var parsedType))
            {
                spec.Where(t => t.TransactionType == parsedType);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TriggeredBy))
        {
            var triggeredBy = filter.TriggeredBy.Trim().ToLower();
            spec.Where(t =>
                (t.PointsRule != null && (t.PointsRule.RuleKey.ToLower().Contains(triggeredBy) || t.PointsRule.EventType.ToLower().Contains(triggeredBy))) ||
                t.TriggeredBy.ToLower().Contains(triggeredBy));
        }

        var sortBy = filter.SortBy ?? "CreatedAt";
        var sortDir = filter.SortDir ?? "Desc";
        var isDescending = sortDir.Equals("Desc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy.ToLower())
        {
            case "createdat":
            case "date":
                spec.OrderBy(t => t.CreatedAt, descending: isDescending);
                break;
            case "transactiontype":
                spec.OrderBy(t => t.TransactionType, descending: isDescending);
                break;
            case "points":
            case "amount":
                spec.OrderBy(t => t.Amount, descending: isDescending);
                break;
            case "employeename":
                spec.OrderBy(t => t.Employee.FirstName + " " + t.Employee.LastName, descending: isDescending);
                break;
            default:
                spec.OrderBy(t => t.CreatedAt, descending: isDescending);
                break;
        }

        var paged = await _pointsTransactionRepository.PagedAsync(spec, pageNumber, pageSize, cancellationToken);

        var items = paged.Items
            .Select(t => new PointsTransactionListItemDto(
                Id: t.Id,
                EmployeeId: t.EmployeeId,
                EmployeeName: t.Employee != null ? (t.Employee.FirstName + " " + t.Employee.LastName).Trim() : string.Empty,
                EmployeeCode: t.Employee != null ? t.Employee.EmployeeCode : string.Empty,
                DepartmentName: t.Employee != null && t.Employee.JobRole != null ? (t.Employee.JobRole.Department != null ? t.Employee.JobRole.Department.Name : string.Empty) : string.Empty,
                SiteName: t.Employee != null && t.Employee.Site != null ? t.Employee.Site.SiteName : string.Empty,
                AvatarUrl: t.Employee != null ? t.Employee.ProfilePhotoUrl : null,
                Date: t.CreatedAt,
                Time: t.CreatedAt.TimeOfDay,
                TransactionType: t.TransactionType.ToString(),
                Points: t.Amount,
                TriggeredBy: t.TriggeredBy,
                Comments: t.Comments,
                CreatedAt: t.CreatedAt
            ))
            .ToList();

        return new PaginatedPointsTransactionsResponseDto(items, paged.TotalCount, paged.PageNumber, paged.PageSize, paged.TotalPages);
    }
}