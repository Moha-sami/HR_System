using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Requests.CreateRequestType;

public record CreateRequestTypeCommand(CreateRequestTypeDto Dto) : IRequest<RequestTypeDto>;
