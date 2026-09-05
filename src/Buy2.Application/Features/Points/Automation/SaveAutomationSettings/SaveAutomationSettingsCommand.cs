using Buy2.Application.DTOs.Points.DTOs;
using MediatR;

namespace Buy2.Application.Features.Points.Automation.SaveAutomationSettings;

public record SaveAutomationSettingsCommand(
    SaveAutomationSettingsDto Request
) : IRequest<SaveAutomationSettingsResult>;

public record SaveAutomationSettingsResult(
    bool IsSuccess,
    int SavedCount = 0,
    string? ErrorMessage = null
);
