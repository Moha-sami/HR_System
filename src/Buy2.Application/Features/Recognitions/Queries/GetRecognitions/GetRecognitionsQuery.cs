using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitions;

public record GetRecognitionsQuery(
    string? Search = null,
    string? Status = null,
    string? SortBy = null,
    string? SortDirection = "desc",
    int PageNumber = 1,
    int PageSize = 10,
    bool IsElevatedUser = false
) : IRequest<PaginatedListResult<RecognitionSummaryDto>>;
