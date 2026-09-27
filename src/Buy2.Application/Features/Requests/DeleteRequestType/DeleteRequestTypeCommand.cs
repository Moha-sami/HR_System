using MediatR;

namespace Buy2.Application.Features.Requests.DeleteRequestType;

public record DeleteRequestTypeCommand(int Id) : IRequest<bool>;
