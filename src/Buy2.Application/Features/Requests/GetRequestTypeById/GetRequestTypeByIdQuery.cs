using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Requests.GetRequestTypeById;

public record GetRequestTypeByIdQuery(int Id) : IRequest<RequestTypeDto>;
