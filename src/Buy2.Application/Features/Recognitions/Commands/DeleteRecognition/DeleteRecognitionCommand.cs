using MediatR;

namespace Buy2.Application.Features.Recognitions.Commands.DeleteRecognition;

public record DeleteRecognitionCommand(
    int Id,
    int? CurrentUserId = null,
    bool IsElevatedUser = false
) : IRequest<bool>;
