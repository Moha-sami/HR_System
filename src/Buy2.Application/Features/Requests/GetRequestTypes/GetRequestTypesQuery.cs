using Buy2.Application.DTOs.Requests;
using MediatR;
using System.Collections.Generic;

namespace Buy2.Application.Features.Requests.GetRequestTypes;

public record GetRequestTypesQuery(
    string? Search = null,
    string? Category = null,
    bool? IsActive = null
) : IRequest<IEnumerable<RequestTypeDto>>;
