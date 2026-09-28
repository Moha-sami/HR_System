using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using MediatR;
using System;

namespace Buy2.Application.Features.News.GetNewsFeed;

public record GetNewsFeedQuery(
    string? Search = null,
    string? Category = null,
    string? Status = null,
    bool IsElevatedUser = false,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedListResult<NewsFeedSummaryDto>>;
