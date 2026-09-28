using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.Requests.ProcessDecision;

public record ProcessRequestDecisionCommand(
    int RequestId,
    string Tier,
    string Decision,
    string? Comment,
    string? RejectionReason,
    int? ReviewerId = null
) : IRequest<ProcessDecisionResponseDto>;
