using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.CreateRequestType;

public class CreateRequestTypeCommandHandler : IRequestHandler<CreateRequestTypeCommand, RequestTypeDto>
{
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRequestTypeCommandHandler(
        IRepository<RequestType> requestTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _requestTypeRepository = requestTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequestTypeDto> Handle(CreateRequestTypeCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto == null)
        {
            throw new ValidationException("Request type payload cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Dto.Name))
        {
            throw new ValidationException("Request type name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Dto.Category))
        {
            throw new ValidationException("Request type category is required.");
        }

        var name = request.Dto.Name.Trim();
        var category = request.Dto.Category.Trim();

        var duplicateExists = await _requestTypeRepository.AnyAsync(
            rt => rt.Category == category && rt.Name == name,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A request type with the name '{name}' already exists in category '{category}'.");
        }

        string? leaveType = null;
        string? leavePay = null;

        var isLeaveCategory = string.Equals(category, "Leave", StringComparison.OrdinalIgnoreCase);

        if (isLeaveCategory)
        {
            if (!string.IsNullOrWhiteSpace(request.Dto.LeaveType))
            {
                var trimmedLeaveType = request.Dto.LeaveType.Trim();
                if (string.Equals(trimmedLeaveType, "Full", StringComparison.OrdinalIgnoreCase))
                {
                    leaveType = "Full";
                }
                else if (string.Equals(trimmedLeaveType, "Partial", StringComparison.OrdinalIgnoreCase))
                {
                    leaveType = "Partial";
                }
                else
                {
                    throw new ValidationException("LeaveType must be either 'Full' or 'Partial'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Dto.LeavePay))
            {
                var trimmedLeavePay = request.Dto.LeavePay.Trim();
                if (string.Equals(trimmedLeavePay, "Paid", StringComparison.OrdinalIgnoreCase))
                {
                    leavePay = "Paid";
                }
                else if (string.Equals(trimmedLeavePay, "Unpaid", StringComparison.OrdinalIgnoreCase))
                {
                    leavePay = "Unpaid";
                }
                else
                {
                    throw new ValidationException("LeavePay must be either 'Paid' or 'Unpaid'.");
                }
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.Dto.LeaveType) || !string.IsNullOrWhiteSpace(request.Dto.LeavePay))
            {
                throw new ValidationException("LeaveType and LeavePay can only be configured for Leave requests.");
            }
        }

        var requestType = new RequestType
        {
            Category = category,
            Name = name,
            Hint = string.IsNullOrWhiteSpace(request.Dto.Hint) ? null : request.Dto.Hint.Trim(),
            LeaveType = leaveType,
            LeavePay = leavePay,
            RequiresDates = request.Dto.RequiresDates,
            RequiresReason = request.Dto.RequiresReason,
            IsActive = request.Dto.IsActive,
            AddedBy = string.IsNullOrWhiteSpace(request.Dto.AddedBy) ? null : request.Dto.AddedBy.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _requestTypeRepository.AddAsync(requestType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestTypeDto(
            requestType.Id,
            requestType.Category,
            requestType.Name,
            requestType.Hint,
            requestType.LeaveType,
            requestType.LeavePay,
            requestType.RequiresDates,
            requestType.RequiresReason,
            requestType.IsActive,
            requestType.CreatedAt,
            requestType.AddedBy
        );
    }
}
