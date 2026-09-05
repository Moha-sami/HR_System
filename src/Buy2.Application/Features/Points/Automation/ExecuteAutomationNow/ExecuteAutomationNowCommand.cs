using Buy2.Application.DTOs.Points.DTOs;
using MediatR;

namespace Buy2.Application.Features.Points.Automation.ExecuteAutomationNow;

public record ExecuteAutomationNowCommand(
    ExecuteAutomationNowDto Request
) : IRequest<ExecuteAutomationNowResult>;

public record ExecuteAutomationNowResult(
    bool IsSuccess,
    AutomationJobResultDto? Data = null,
    string? ErrorMessage = null,
    bool IsNotFound = false,
    string? Message = null
);
