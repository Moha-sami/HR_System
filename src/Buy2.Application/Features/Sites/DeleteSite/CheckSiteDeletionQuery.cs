using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Buy2.Application.Features.Sites.DeleteSite;

public record CheckSiteDeletionQuery(int SiteId) : IRequest<DeletionCheckDto>;
public class CheckSiteDeletionQueryHandler : IRequestHandler<CheckSiteDeletionQuery, DeletionCheckDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public CheckSiteDeletionQueryHandler(IRepository<Site> siteRepository, IRepository<Employee> employeeRepository)
    {
        _siteRepository = siteRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<DeletionCheckDto> Handle(CheckSiteDeletionQuery query, CancellationToken cancellationToken)
    {
        var site = await _siteRepository
            .FirstOrDefaultAsync(
                s => s.Id == query.SiteId,
                cancellationToken,
                "EmployeeSites",
                "EmployeeSites.Employee",
                nameof(Site.Shifts));

        if (site is null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var primaryEmployees = await _employeeRepository
            .ListAsync(e => e.SiteId == query.SiteId, cancellationToken);

        var allocatedEmployees = primaryEmployees
            .Concat(site.EmployeeSites.Where(es => es.Employee != null).Select(es => es.Employee!))
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .Select(e => new AllocatedEmployeeDto(
                e.Id,
                $"{e.FirstName} {e.LastName}".Trim()
            ))
            .ToList();

        var allocatedEmployeeCount = allocatedEmployees.Count;

        var futureShiftCount = site.Shifts
            .Count(s => s.StartTime > DateTimeOffset.UtcNow);

        var canDelete = allocatedEmployeeCount == 0 && futureShiftCount == 0;

        return new DeletionCheckDto(
            canDelete,
            allocatedEmployeeCount,
            allocatedEmployees
        );
    }
}