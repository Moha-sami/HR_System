using Buy2.Application.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Requests.SubmitRequest;

public record SubmitRequestCommand(
    int EmployeeId,
    int RequestTypeId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Reason,
    string? CategoryValuesJson,
    IReadOnlyList<IFormFile>? Attachments = null
) : IRequest<SubmittedRequestResponseDto>;
