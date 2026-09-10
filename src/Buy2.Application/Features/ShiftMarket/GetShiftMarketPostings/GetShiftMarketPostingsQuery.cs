using Buy2.Application.DTOs.ShiftMarket;
using MediatR;

namespace Buy2.Application.Features.ShiftMarket.GetShiftMarketPostings;

public record GetShiftMarketPostingsQuery(
    string? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10,
    int? SiteId = null
) : IRequest<ShiftMarketPaginatedResponseDto>;
