using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Qualifications.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Qualifications.GetQualifications;

public record GetQualificationsQuery(string? Search = null, string? Type = null) : IRequest<IEnumerable<QualificationLookupDto>>;

public class GetQualificationsQueryHandler : IRequestHandler<GetQualificationsQuery, IEnumerable<QualificationLookupDto>>
{
    private readonly IRepository<Qualification> _qualificationRepository;

    public GetQualificationsQueryHandler(IRepository<Qualification> qualificationRepository)
    {
        _qualificationRepository = qualificationRepository;
    }

    public async Task<IEnumerable<QualificationLookupDto>> Handle(GetQualificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _qualificationRepository.Query(asNoTracking: true);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.Trim().ToLower();
            query = query.Where(q => q.Name.ToLower().Contains(searchLower) || (q.Description != null && q.Description.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var typeLower = request.Type.Trim().ToLower();
            query = query.Where(q => q.Category.ToLower() == typeLower);
        }

        return await query
            .Select(q => new QualificationLookupDto(
                q.Id,
                q.Name,
                q.Category,
                q.Description,
                false
            ))
            .ToListAsync(cancellationToken);
    }
}
