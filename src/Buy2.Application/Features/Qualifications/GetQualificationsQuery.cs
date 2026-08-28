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

public record GetQualificationsQuery() : IRequest<IEnumerable<QualificationLookupDto>>;

public class GetQualificationsQueryHandler : IRequestHandler<GetQualificationsQuery, IEnumerable<QualificationLookupDto>>
{
    private readonly IRepository<Qualification> _qualificationRepository;

    public GetQualificationsQueryHandler(IRepository<Qualification> qualificationRepository)
    {
        _qualificationRepository = qualificationRepository;
    }

    public async Task<IEnumerable<QualificationLookupDto>> Handle(GetQualificationsQuery request, CancellationToken cancellationToken)
    {
        var qualifications = await _qualificationRepository.Query(asNoTracking: true)
            .Select(q => new QualificationLookupDto(
                q.Id,
                q.Name,
                q.Category,
                q.Description,
                false // Assuming IsSystem is false by default since there's no DB column for it
            ))
            .ToListAsync(cancellationToken);

        return qualifications;
    }
}
