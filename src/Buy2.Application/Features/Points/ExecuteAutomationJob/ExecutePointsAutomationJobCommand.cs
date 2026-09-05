using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using MediatR;

namespace Buy2.Application.Features.Points.ExecuteAutomationJob;

public record ExecutePointsAutomationJobCommand(
    ExecuteAutomationJobDto Request
) : IRequest<ExecutePointsAutomationJobResult>;

public record ExecutePointsAutomationJobResult(
    bool IsSuccess,
    AutomationJobResultDto? Data = null,
    string? ErrorMessage = null,
    bool IsNotFound = false
);