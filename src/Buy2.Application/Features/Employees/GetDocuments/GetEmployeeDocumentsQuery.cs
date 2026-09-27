using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetDocuments;

public record GetEmployeeDocumentsQuery(int EmployeeId) : IRequest<List<EmployeeDocumentDto>>;

public class GetEmployeeDocumentsQueryHandler : IRequestHandler<GetEmployeeDocumentsQuery, List<EmployeeDocumentDto>>
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;

    public GetEmployeeDocumentsQueryHandler(IRepository<EmployeeDocument> employeeDocumentRepository)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
    }

    public async Task<List<EmployeeDocumentDto>> Handle(GetEmployeeDocumentsQuery request, CancellationToken cancellationToken)
    {
        return await _employeeDocumentRepository
            .Query(asNoTracking: true)
            .Where(d => d.EmployeeId == request.EmployeeId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new EmployeeDocumentDto(
                d.Id,
                d.EmployeeId,
                d.Category,
                d.StorageUrl,
                d.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
