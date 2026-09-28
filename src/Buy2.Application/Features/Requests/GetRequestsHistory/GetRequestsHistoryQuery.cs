using Buy2.Application.DTOs.Requests;
using MediatR;
using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Requests.GetRequestsHistory;

public record GetRequestsHistoryQuery(
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    List<int>? RequestTypeIds = null,
    string? OverallStatus = null,
    string? SortBy = "SubmittedAt",
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedListResult<RequestHistorySummaryDto>>;
