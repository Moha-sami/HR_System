using Buy2.Application.DTOs.ShiftMarket;
using MediatR;

namespace Buy2.Application.Features.ShiftMarket.ApproveShiftClaim;

public record ApproveShiftClaimCommand(int ClaimId) : IRequest<ApproveShiftClaimResponseDto>;
