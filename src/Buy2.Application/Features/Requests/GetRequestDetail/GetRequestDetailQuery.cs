using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Requests.GetRequestDetail;

public record GetRequestDetailQuery(int Id) : IRequest<RequestDetailDto>;
