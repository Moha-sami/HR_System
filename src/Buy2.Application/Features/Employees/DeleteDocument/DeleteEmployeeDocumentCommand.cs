using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.DeleteDocument;

public record DeleteEmployeeDocumentCommand(int EmployeeId, int DocumentId) : IRequest<bool>;

public class DeleteEmployeeDocumentCommandHandler : IRequestHandler<DeleteEmployeeDocumentCommand, bool>
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmployeeDocumentCommandHandler(
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _employeeDocumentRepository
            .Query(asNoTracking: false)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EmployeeId == request.EmployeeId, cancellationToken);

        if (document is null)
        {
            return false;
        }

        _employeeDocumentRepository.Delete(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
