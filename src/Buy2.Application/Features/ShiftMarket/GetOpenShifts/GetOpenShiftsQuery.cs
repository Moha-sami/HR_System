using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.ShiftMarket.GetOpenShifts;

public record GetOpenShiftsQuery() : IRequest<List<ShiftDto>>;

public class GetOpenShiftsQueryHandler : IRequestHandler<GetOpenShiftsQuery, List<ShiftDto>>
{
    private readonly IRepository<ShiftEntity> _shiftRepository;

    public GetOpenShiftsQueryHandler(IRepository<ShiftEntity> shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<List<ShiftDto>> Handle(GetOpenShiftsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var shifts = await _shiftRepository.ListAsync(
            s => s.IsPublished && s.EmployeeId == null && s.StartTime > now,
            cancellationToken);

        return shifts
            .Select(s => new ShiftDto(
                s.Id,
                s.EmployeeId,
                s.SiteId,
                s.JobRoleId,
                s.StartTime,
                s.EndTime,
                s.IsPublished
            ))
            .ToList();
    }
}
