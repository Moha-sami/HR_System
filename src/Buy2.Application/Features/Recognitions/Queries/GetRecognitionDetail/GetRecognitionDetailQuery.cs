using MediatR;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitionDetail;

public record GetRecognitionDetailQuery(
    int Id,
    int? CurrentUserId = null,
    bool IsElevatedUser = false
) : IRequest<RecognitionDetailDto>;
