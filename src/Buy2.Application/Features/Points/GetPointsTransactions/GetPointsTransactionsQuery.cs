using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        var query = _pointsTransactionRepository.Query()
            .AsNoTracking()
            .Where(t => !t.Employee.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
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

            query = query.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);
        }
        else
        {
            if (filter.DateFrom.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= filter.DateTo.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType))
        {
            var type = filter.TransactionType.Trim();
            if (Enum.TryParse<TransactionType>(type, true, out var parsedType))
            {
                query = query.Where(t => t.TransactionType == parsedType);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TriggeredBy))
        {
            var triggeredBy = filter.TriggeredBy.Trim().ToLower();
            query = query.Where(t =>
                (t.PointsRule != null && (t.PointsRule.RuleKey.ToLower().Contains(triggeredBy) || t.PointsRule.EventType.ToLower().Contains(triggeredBy))) ||
                t.TriggeredBy.ToLower().Contains(triggeredBy));
        }

        var sortBy = filter.SortBy ?? "CreatedAt";
        var sortDir = filter.SortDir ?? "Desc";
        var isDescending = sortDir.Equals("Desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy.ToLower() switch
        {
            "createdat" or "date" => isDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "transactiontype" => isDescending ? query.OrderByDescending(t => t.TransactionType) : query.OrderBy(t => t.TransactionType),
            "points" or "amount" => isDescending ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
            "employeename" => isDescending ? query.OrderByDescending(t => t.Employee.FirstName + " " + t.Employee.LastName) : query.OrderBy(t => t.Employee.FirstName + " " + t.Employee.LastName),
            _ => isDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PointsTransactionListItemDto(
                Id: t.Id,
                EmployeeId: t.EmployeeId,
                EmployeeName: (t.Employee.FirstName + " " + t.Employee.LastName).Trim(),
                EmployeeCode: t.Employee.EmployeeCode,
                DepartmentName: t.Employee.JobRole != null ? (t.Employee.JobRole.Department != null ? t.Employee.JobRole.Department.Name : string.Empty) : string.Empty,
                SiteName: t.Employee.Site != null ? t.Employee.Site.SiteName : string.Empty,
                AvatarUrl: t.Employee.ProfilePhotoUrl,
                Date: t.CreatedAt,
                Time: t.CreatedAt.TimeOfDay,
                TransactionType: t.TransactionType.ToString(),
                Points: t.Amount,
                TriggeredBy: t.TriggeredBy,
                Comments: t.Comments,
                CreatedAt: t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedPointsTransactionsResponseDto(items, totalCount, pageNumber, pageSize, totalPages);
    }
}