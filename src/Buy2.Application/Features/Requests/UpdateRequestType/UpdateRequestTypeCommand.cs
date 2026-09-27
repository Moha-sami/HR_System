using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Requests.UpdateRequestType;

public record UpdateRequestTypeCommand(int Id, UpdateRequestTypeDto Dto) : IRequest<RequestTypeDto>;
