using Buy2.Application.DTOs.Requests;
using MediatR;
using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Requests.ExportRequestsHistory;

public record ExportRequestsHistoryQuery(
    string Format = "csv",
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    List<int>? RequestTypeIds = null,
    string? OverallStatus = null,
    string? SortBy = "SubmittedAt",
    bool SortDescending = true
) : IRequest<ExportRequestsHistoryResult>;
