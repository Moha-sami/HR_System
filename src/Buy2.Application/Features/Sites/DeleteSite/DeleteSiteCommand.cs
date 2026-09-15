using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Buy2.Application.Features.Sites.DeleteSite;

public record DeleteSiteCommand(
    int SiteId,
    List<EmployeeSiteReassignmentDto>? EmployeeSiteReassignments
) : IRequest;
public class DeleteSiteCommandHandler : IRequestHandler<DeleteSiteCommand>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSiteCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSiteCommand command, CancellationToken cancellationToken)
    {
        var site = await _siteRepository
            .FirstOrDefaultAsync(
                new Specification<Site>()
                    .Where(s => s.Id == command.SiteId)
                    .Include(nameof(Site.EmployeeSites), nameof(Site.Shifts))
                    .AsTracked(),
                cancellationToken);

        if (site is null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var primaryEmployeeIds = await _employeeRepository
            .ListAsync(
                new Specification<Employee>().Where(e => e.SiteId == command.SiteId),
                e => e.Id,
                cancellationToken);

        var allocatedEmployeeIds = primaryEmployeeIds
            .Concat(site.EmployeeSites.Select(es => es.EmployeeId))
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
                .Where(id => !reassignedEmployeeIds.Contains(id))
                .ToList();

            if (missingEmployees.Count > 0)
            {
                throw new ValidationException("All assigned employees must have a replacement site.");
            }

            var newSiteIds = reassignment
                .Select(r => r.NewSiteId)
                .Distinct()
                .ToList();

            var validateNewSiteId = await _siteRepository
                .ListAsync(
                    new Specification<Site>().Where(s => newSiteIds.Contains(s.Id)),
                    s => s.Id,
                    cancellationToken);

            if (validateNewSiteId.Count != newSiteIds.Count)
            {
                throw new ValidationException("One or more replacement sites do not exist.");
            }

            if (newSiteIds.Contains(command.SiteId))
            {
                throw new ValidationException("Employees cannot be reallocated to the site being deleted.");
            }

            var employeesToReallocate = await _employeeRepository
                .ListAsync(
                    new Specification<Employee>()
                        .Where(e => reassignedEmployeeIds.Contains(e.Id))
                        .AsTracked(),
                    cancellationToken);

            foreach (var emp in employeesToReallocate)
            {
                var target = reassignment.First(r => r.EmployeeId == emp.Id);
                emp.SiteId = target.NewSiteId;
            }
        }

        _siteRepository.Delete(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

