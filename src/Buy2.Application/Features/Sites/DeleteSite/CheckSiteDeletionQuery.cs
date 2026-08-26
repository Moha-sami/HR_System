using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.DeleteSite;

public record CheckSiteDeletionQuery(int SiteId):IRequest<DeletionCheckDto>;
public class CheckSiteDeletionQueryHandler : IRequestHandler<CheckSiteDeletionQuery, DeletionCheckDto>
{
    private readonly IRepository<Site> _siteRepository;
    public CheckSiteDeletionQueryHandler(IRepository<Site> siteRepository)
    {
        _siteRepository = siteRepository;
    }
    public async Task<DeletionCheckDto> Handle(CheckSiteDeletionQuery query, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .Query(false)
            .Include(s => s.EmployeeSites) 
            .ThenInclude(es => es.Employee)
            .Include(s => s.Shifts)
            .FirstOrDefaultAsync(s => s.Id == query.SiteId, cancellation);
        if (site is null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var allocatedEmployee = site.EmployeeSites
            .Select(s => new AllocatedEmployeeDto(
                    s.EmployeeId,
                    $"{s.Employee?.FirstName ?? string.Empty} {s.Employee?.LastName ?? string.Empty}".Trim()
                )).ToList();

        var allocatedEmployeeCount = allocatedEmployee.Count;

        var futureShiftCount = site.Shifts
            .Count(s => s.StartTime > DateTimeOffset.UtcNow);

        var canDelete = 
            allocatedEmployeeCount == 0 && futureShiftCount == 0;

        return new DeletionCheckDto(
                canDelete,
                allocatedEmployeeCount,
                allocatedEmployee
            );
    }
}