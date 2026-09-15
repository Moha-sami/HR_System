using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.Departments.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Departments.GetDepartments;

public record GetDepartmentsQuery() : IRequest<IEnumerable<DepartmentLookupDto>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IEnumerable<DepartmentLookupDto>>
{
    private readonly IRepository<Department> _departmentRepository;

    public GetDepartmentsQueryHandler(IRepository<Department> departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IEnumerable<DepartmentLookupDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var spec = new Specification<Department>()
            .Include(nameof(Department.HeadEmployee), nameof(Department.JobRoles));

        var entities = await _departmentRepository.ListAsync(spec, cancellationToken);

        var departments = entities.Select(d => new DepartmentLookupDto(
                d.Id,
                d.Name,
                d.Description,
                d.JobRoles != null ? d.JobRoles.Count(j => j.IsActive) : 0,
                d.HeadEmployee != null ? d.HeadEmployee.FirstName + " " + d.HeadEmployee.LastName : null
            )).ToList();

        return departments;
    }
}
