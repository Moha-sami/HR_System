using MediatR;

namespace Buy2.Application.Features.Employees.UploadDocument;

public record UploadEmployeeDocumentCommand(
    int EmployeeId,
    string Category,
    string StorageUrl
    ) : IRequest<int>;