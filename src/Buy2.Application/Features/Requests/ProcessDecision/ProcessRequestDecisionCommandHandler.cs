using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.ProcessDecision;

public class ProcessRequestDecisionCommandHandler : IRequestHandler<ProcessRequestDecisionCommand, ProcessDecisionResponseDto>
{
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessRequestDecisionCommandHandler(
        IRepository<Request> requestRepository,
        IRepository<Notification> notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _requestRepository = requestRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProcessDecisionResponseDto> Handle(
        ProcessRequestDecisionCommand request,
        CancellationToken cancellationToken)
    {
        var requestEntity = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (requestEntity == null)
        {
            throw new KeyNotFoundException($"Request with ID {request.RequestId} was not found.");
        }

        if (string.Equals(requestEntity.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(requestEntity.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Request #{request.RequestId} has already been resolved with status '{requestEntity.Status}'.");
        }

        var tier = request.Tier.Trim();
        var isManager = string.Equals(tier, "Manager", StringComparison.OrdinalIgnoreCase);
        var isHr = string.Equals(tier, "HR", StringComparison.OrdinalIgnoreCase);

        if (!isManager && !isHr)
        {
            throw new ValidationException("Tier must be either 'Manager' or 'HR'.");
        }

        var decision = request.Decision.Trim();
        var isApproved = string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase);
        var isRejected = string.Equals(decision, "Rejected", StringComparison.OrdinalIgnoreCase);

        if (!isApproved && !isRejected)
        {
            throw new ValidationException("Decision must be either 'Approved' or 'Rejected'.");
        }

        if (isRejected)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                throw new ValidationException("Comment is required when rejecting a request.");
            }
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                throw new ValidationException("Rejection reason is required when rejecting a request.");
            }
        }

        // 1. Update Decision Gate
        if (isManager)
        {
            requestEntity.ManagerStatus = isApproved ? "Approved" : "Rejected";
            requestEntity.ManagerComment = request.Comment?.Trim();
            requestEntity.ManagerDecisionAt = DateTime.UtcNow;
            if (request.ReviewerId.HasValue)
            {
                requestEntity.ManagerId = request.ReviewerId.Value;
                requestEntity.ApproverId = request.ReviewerId.Value;
            }
        }
        else // HR
        {
            requestEntity.HrStatus = isApproved ? "Approved" : "Rejected";
            requestEntity.HrComment = request.Comment?.Trim();
            requestEntity.HrDecisionAt = DateTime.UtcNow;
            if (request.ReviewerId.HasValue)
            {
                requestEntity.HrId = request.ReviewerId.Value;
            }
        }

        // 2. Aggregate Overall Status Transition (AC 3)
        // If either tier rejects -> request becomes Rejected
        // If both approve -> request becomes Approved
        // Otherwise -> remains Pending
        if (string.Equals(requestEntity.ManagerStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(requestEntity.HrStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            requestEntity.Status = "Rejected";
            requestEntity.ResolvedAt = DateTime.UtcNow;
            requestEntity.RejectionReason = request.RejectionReason?.Trim() ?? requestEntity.RejectionReason;
        }
        else if (string.Equals(requestEntity.ManagerStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(requestEntity.HrStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            requestEntity.Status = "Approved";
            requestEntity.ResolvedAt = DateTime.UtcNow;
            requestEntity.RejectionReason = null;
        }
        else
        {
            requestEntity.Status = "Pending";
            requestEntity.ResolvedAt = null;
        }

        // 3. Commit DB changes atomically
        _requestRepository.Update(requestEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Dispatch notification to employee outside/after main transaction commit (AC 4, 5)
        try
        {
            var notification = new Notification
            {
                EmployeeId = requestEntity.EmployeeId,
                Title = $"Request #{requestEntity.Id} Update",
                Body = $"Your request review by {tier} resulted in: {decision}. Overall status is now {requestEntity.Status}.",
                Type = "REQUEST_REVIEW",
                ReferenceId = requestEntity.Id.ToString(),
                ReferenceType = "Request",
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Do not fail the successful DB state if notification dispatch encounters an issue
        }

        return new ProcessDecisionResponseDto(
            RequestId: requestEntity.Id,
            OverallStatus: requestEntity.Status,
            ManagerStatus: requestEntity.ManagerStatus,
            ManagerComment: requestEntity.ManagerComment,
            ManagerDecisionAt: requestEntity.ManagerDecisionAt,
            HrStatus: requestEntity.HrStatus,
            HrComment: requestEntity.HrComment,
            HrDecisionAt: requestEntity.HrDecisionAt,
            RejectionReason: requestEntity.RejectionReason,
            ResolvedAt: requestEntity.ResolvedAt
        );
    }
}
