using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Sites.GetSiteDetails;

public record GetSiteEmployeesQuery(int SiteId) : IRequest<List<EmployeeTabDto>>;
public class GetSiteEmployeesQueryHandler : IRequestHandler<GetSiteEmployeesQuery, List<EmployeeTabDto>>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Employee> _employeeRepository;
    public GetSiteEmployeesQueryHandler(IRepository<Site> site, IRepository<Employee> employee)
    {
        _siteRepository = site;
        _employeeRepository = employee;
    }
    public async Task<List<EmployeeTabDto>> Handle(GetSiteEmployeesQuery query, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .AnyAsync(s => s.Id == query.SiteId, cancellation);
        if (!site)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var spec = new Specification<Employee>()
            .IgnoreFilters()
            .Include(nameof(Employee.JobRole))
            .Where(employee => employee.SiteId == query.SiteId ||
                   employee.EmployeeSites.Any(es => es.SiteId == query.SiteId));
        var employees = await _employeeRepository.ListAsync(spec, cancellation);

        return employees
            .Select(e => new EmployeeTabDto(
                e.Id,
                $"{e.FirstName} {e.LastName}",
                e.JobRole?.Title ?? string.Empty,
                e.Email,
                e.PhoneNumber,
                GetEmployeeStatus(e)
                )).ToList();
    }
    public static string GetEmployeeStatus(Employee e)
    {
        if (e.IsDeleted)
        {
            return "Terminated";
        }
        if (!e.IsActive)
        {
            return "Suspended";
        }
        return "Active";
    }
}
