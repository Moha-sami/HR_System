using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.DeleteSite;

public record DeleteSiteCommand(
    int SiteId,
    List<EmployeeSiteReassignmentDto>? EmployeeSiteReassignments
) : IRequest;
public class DeleteSiteCommandHandler : IRequestHandler<DeleteSiteCommand>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteSiteCommandHandler(IRepository<Site> site, IUnitOfWork unit)
    {
        _siteRepository = site;
        _unitOfWork = unit;
    }

    public async Task Handle(DeleteSiteCommand command, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .Query()
            .Include(s => s.EmployeeSites)
            .Include(s => s.Shifts)
            .FirstOrDefaultAsync(s => s.Id == command.SiteId, cancellation);

        if(site is null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var allocatedEmployeeIds = site.EmployeeSites
            .Select(es => es.EmployeeId)
            .Distinct()
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var futureShifts = site.Shifts
            .Any(s => s.StartTime > now);
            
        if (futureShifts)
        {
            throw new ValidationException("Site cannot be deleted because it has future scheduled shifts.");
        }
        if (allocatedEmployeeIds.Count > 0)
        {
            if (command.EmployeeSiteReassignments is null ||
                command.EmployeeSiteReassignments.Count == 0)
            {
                throw new ValidationException("All assigned employees must be reallocated before deleting the site.");
            }

            var reassignment = command.EmployeeSiteReassignments.ToList();

            var duplicateEmployeeIds = reassignment
                .GroupBy(r => r.EmployeeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateEmployeeIds.Count > 0)
            {
                throw new ValidationException(
                    "An employee cannot have multiple replacement sites.");
            }

            var reassignedEmployeeIds = reassignment
                .Select(s => s.EmployeeId)
                .ToHashSet();

            var invalidEmployeeIds = reassignedEmployeeIds
                .Except(allocatedEmployeeIds)
                .ToList();

            if (invalidEmployeeIds.Count > 0)
            {
                throw new ValidationException(
                    "One or more employees are not assigned to this site.");
            }

            var missingEmployees = allocatedEmployeeIds
                .Where(Id => !reassignedEmployeeIds.Contains(Id))
                .ToList();

            if(missingEmployees.Count > 0)
            {
                throw new ValidationException("All assigned employees must have a replacement site.");
            }

            var newSiteIds = reassignment
                .Select(r => r.NewSiteId)
                .Distinct()
                .ToList();

            var validateNewSiteId = await _siteRepository
                .Query()
                .Where(s => newSiteIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(cancellation);
            if(validateNewSiteId.Count != newSiteIds.Count)
            {
                throw new ValidationException("One or more replacement sites do not exist.");
            }

            if (newSiteIds.Contains(command.SiteId))
            {
                throw new ValidationException("Employees cannot be reallocated to the site being deleted.");
            }

            foreach (var employee in site.EmployeeSites)
            {
                var checkRessignment = reassignment
                    .First(r => r.EmployeeId == employee.EmployeeId);

                employee.SiteId = checkRessignment.NewSiteId; 
            }
        }
        _siteRepository.Delete(site);
        await _unitOfWork.SaveChangesAsync(cancellation);
    }
}

