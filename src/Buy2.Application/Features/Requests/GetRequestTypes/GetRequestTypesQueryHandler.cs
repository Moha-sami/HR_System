using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.GetRequestTypes;

public class GetRequestTypesQueryHandler : IRequestHandler<GetRequestTypesQuery, IEnumerable<RequestTypeDto>>
{
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IRepository<Request> _requestRepository;

    public GetRequestTypesQueryHandler(
        IRepository<RequestType> requestTypeRepository,
        IRepository<Request> requestRepository)
    {
        _requestTypeRepository = requestTypeRepository;
        _requestRepository = requestRepository;
    }

    public async Task<IEnumerable<RequestTypeDto>> Handle(GetRequestTypesQuery request, CancellationToken cancellationToken)
    {
        var query = _requestTypeRepository.Query(asNoTracking: true);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(rt => rt.Category == category);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(rt => rt.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(rt => rt.Name.Contains(search) 
                                   || rt.Category.Contains(search) 
                                   || (rt.Hint != null && rt.Hint.Contains(search)));
        }

        query = query.OrderBy(rt => rt.Category).ThenBy(rt => rt.Name);

        if (request.PageNumber.HasValue && request.PageSize.HasValue && request.PageNumber > 0 && request.PageSize > 0)
        {
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value).Take(request.PageSize.Value);
        }

        var items = await query.ToListAsync(cancellationToken);

        var utilizedTypeIds = await _requestRepository.Query(asNoTracking: true)
            .Select(r => r.RequestTypeId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var utilizedSet = new HashSet<int>(utilizedTypeIds);

        var list = items.Select(rt => new RequestTypeDto(
            rt.Id,
            rt.Category,
            rt.Name,
            rt.Hint,
            rt.LeaveType,
            rt.LeavePay,
            rt.RequiresDates,
            rt.RequiresReason,
            rt.IsActive,
            rt.CreatedAt,
            rt.AddedBy,
            utilizedSet.Contains(rt.Id)
        )).ToList();

        return list;
    }
}
