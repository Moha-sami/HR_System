using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.GetRequestTypeById;

public class GetRequestTypeByIdQueryHandler : IRequestHandler<GetRequestTypeByIdQuery, RequestTypeDto>
{
    private readonly IRepository<RequestType> _requestTypeRepository;

    public GetRequestTypeByIdQueryHandler(IRepository<RequestType> requestTypeRepository)
    {
        _requestTypeRepository = requestTypeRepository;
    }

    public async Task<RequestTypeDto> Handle(GetRequestTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var requestType = await _requestTypeRepository.Query(asNoTracking: true)
            .FirstOrDefaultAsync(rt => rt.Id == request.Id, cancellationToken);

        if (requestType == null)
        {
            throw new KeyNotFoundException($"Request type with ID {request.Id} was not found.");
        }

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
