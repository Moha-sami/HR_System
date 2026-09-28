using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.SubmitRequest;

public class SubmitRequestCommandHandler : IRequestHandler<SubmitRequestCommand, SubmittedRequestResponseDto>
{
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService? _fileStorageService;

    public SubmitRequestCommandHandler(
        IRepository<Request> requestRepository,
        IRepository<Employee> employeeRepository,
        IRepository<RequestType> requestTypeRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService? fileStorageService = null)
    {
        _requestRepository = requestRepository;
        _employeeRepository = employeeRepository;
        _requestTypeRepository = requestTypeRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<SubmittedRequestResponseDto> Handle(
        SubmitRequestCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify Employee exists and resolve direct manager
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {request.EmployeeId} was not found.");
        }

        // 2. Verify RequestType exists and check operational requirements
        var requestType = await _requestTypeRepository.GetByIdAsync(request.RequestTypeId, cancellationToken);
        if (requestType == null)
        {
            throw new KeyNotFoundException($"Request type with ID {request.RequestTypeId} was not found.");
        }

        if (!requestType.IsActive)
        {
            throw new InvalidOperationException($"Request type '{requestType.Name}' is not currently active.");
        }

        if (requestType.RequiresDates && (!request.StartDate.HasValue || !request.EndDate.HasValue))
        {
            throw new ValidationException($"Request type '{requestType.Name}' requires start and end dates.");
        }

        if (requestType.RequiresReason && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException($"Request type '{requestType.Name}' requires a justification reason.");
        }

        // 3. Resolve direct manager
        int? managerId = employee.DirectManagerId;
        string? managerName = null;
        if (managerId.HasValue)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerId.Value, cancellationToken);
            if (manager != null)
            {
                managerName = $"{manager.FirstName} {manager.LastName}".Trim();
            }
        }

        // 4. Instantiate Request aggregate with initial Pending states
        var requestEntity = new Request
        {
            EmployeeId = employee.Id,
            RequestTypeId = requestType.Id,
            ManagerId = managerId,
            ApproverId = managerId, // Backwards compatibility with Approver
            Status = "Pending",
            ManagerStatus = "Pending",
            HrStatus = "Pending",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CategoryValuesJson = string.IsNullOrWhiteSpace(request.CategoryValuesJson) ? null : request.CategoryValuesJson.Trim(),
            SubmittedAt = DateTime.UtcNow
        };

        // 5. Handle file attachments
        if (request.Attachments != null && request.Attachments.Count > 0)
        {
            foreach (var file in request.Attachments)
            {
                if (file.Length > 0)
                {
                    string storageUrl;
                    if (_fileStorageService != null)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        storageUrl = await _fileStorageService.UploadAsync(uniqueFileName, file);
                    }
                    else
                    {
                        storageUrl = $"/storage/requests/{Guid.NewGuid()}_{file.FileName}";
                    }

                    requestEntity.Attachments.Add(new RequestAttachment
                    {
                        FileName = file.FileName,
                        StorageUrl = storageUrl,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // 6. Stage and commit atomically
        await _requestRepository.AddAsync(requestEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Project response
        var attachmentDtos = requestEntity.Attachments.Select(a => new RequestAttachmentDto(
            a.Id,
            a.FileName,
            a.StorageUrl,
            a.ContentType,
            a.FileSize,
            a.UploadedAt
        )).ToList();

        return new SubmittedRequestResponseDto(
            requestEntity.Id,
            employee.Id,
            requestType.Id,
            requestType.Name,
            requestType.Category,
            requestEntity.Status,
            requestEntity.ManagerStatus,
            requestEntity.HrStatus,
            managerId,
            managerName,
            requestEntity.SubmittedAt,
            requestEntity.StartDate,
            requestEntity.EndDate,
            requestEntity.Reason,
            requestEntity.CategoryValuesJson,
            attachmentDtos
        );
    }
}
