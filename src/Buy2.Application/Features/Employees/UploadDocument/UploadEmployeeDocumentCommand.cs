using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Employees.UploadDocument;

public record UploadEmployeeDocumentCommand(
    int EmployeeId,
    string Category,
    string StorageUrl
    ) : IRequest<int>;

public class UploadEmployeeDocumentCommandHandler : IRequestHandler<UploadEmployeeDocumentCommand, int>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadEmployeeDocumentCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IUnitOfWork unitOfWork
        )
    {
        _employeeRepository = employeeRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(UploadEmployeeDocumentCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new ValidationException($"Employee with Id {command.EmployeeId} does not exist.");
        }

        var employeeDocument = new EmployeeDocument
        {
            EmployeeId = command.EmployeeId,
            Category = command.Category,
            StorageUrl = command.StorageUrl
        };

        await _employeeDocumentRepository.AddAsync(employeeDocument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return employeeDocument.Id;
    }
}