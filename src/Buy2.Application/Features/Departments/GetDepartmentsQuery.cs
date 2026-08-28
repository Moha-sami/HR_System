using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Departments.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var departments = await _departmentRepository.Query(asNoTracking: true)
            .Include(d => d.HeadEmployee)
            .Include(d => d.JobRoles)
            .Select(d => new DepartmentLookupDto(
                d.Id,
                d.Name,
                d.Description,
                d.JobRoles.Count(j => j.IsActive),
                d.HeadEmployee != null ? d.HeadEmployee.FirstName + " " + d.HeadEmployee.LastName : null
            ))
            .ToListAsync(cancellationToken);

        return departments;
    }
}
