using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.GetRequestDetail;

public class GetRequestDetailQueryHandler : IRequestHandler<GetRequestDetailQuery, RequestDetailDto>
{
    private readonly IRepository<Request> _requestRepository;

    public GetRequestDetailQueryHandler(IRepository<Request> requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<RequestDetailDto> Handle(
        GetRequestDetailQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _requestRepository.Query(asNoTracking: true)
            .Include(r => r.Employee)
                .ThenInclude(e => e!.JobRole)
            .Include(r => r.RequestType)
            .Include(r => r.Manager)
            .Include(r => r.Hr)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Request with ID {request.Id} was not found.");
        }

        // Secondary query: Retrieve historical requests submitted by this employee
        var previousRequests = await _requestRepository.Query(asNoTracking: true)
            .Include(r => r.RequestType)
            .Where(r => r.EmployeeId == entity.EmployeeId && r.Id != entity.Id)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new PreviousRequestSummaryDto(
                r.Id,
                r.RequestType != null ? r.RequestType.Name : "Unknown",
                r.RequestType != null ? r.RequestType.Category : "General",
                r.SubmittedAt,
                r.StartDate,
                r.EndDate,
                r.Status,
                r.ResolvedAt
            ))
            .ToListAsync(cancellationToken);

        var attachments = entity.Attachments.Select(a => new RequestAttachmentDto(
            a.Id,
            a.FileName,
            a.StorageUrl,
            a.ContentType,
            a.FileSize,
            a.UploadedAt
        )).ToList();

        var managerReview = new RequestReviewNoteDto(
            ReviewerRole: "Manager",
            ReviewerId: entity.ManagerId,
            ReviewerName: entity.Manager != null ? $"{entity.Manager.FirstName} {entity.Manager.LastName}".Trim() : null,
            Status: entity.ManagerStatus,
            Comment: entity.ManagerComment,
            DecisionAt: entity.ManagerDecisionAt
        );

        var hrReview = new RequestReviewNoteDto(
            ReviewerRole: "HR",
            ReviewerId: entity.HrId,
            ReviewerName: entity.Hr != null ? $"{entity.Hr.FirstName} {entity.Hr.LastName}".Trim() : null,
            Status: entity.HrStatus,
            Comment: entity.HrComment,
            DecisionAt: entity.HrDecisionAt
        );

        return new RequestDetailDto(
            Id: entity.Id,
            EmployeeId: entity.EmployeeId,
            EmployeeName: entity.Employee != null ? $"{entity.Employee.FirstName} {entity.Employee.LastName}".Trim() : "Unknown",
            EmployeeCode: entity.Employee != null ? entity.Employee.NationalId : null,
            DepartmentName: entity.Employee?.JobRole != null ? entity.Employee.JobRole.Title : null,
            RequestType: entity.RequestType != null ? entity.RequestType.Name : "Unknown",
            Category: entity.RequestType != null ? entity.RequestType.Category : "General",
            SubmittedAt: entity.SubmittedAt,
            StartDate: entity.StartDate,
            EndDate: entity.EndDate,
            Reason: entity.Reason,
            CategoryValuesJson: entity.CategoryValuesJson,
            OverallStatus: entity.Status,
            RejectionReason: entity.RejectionReason,
            ManagerReview: managerReview,
            HrReview: hrReview,
            Attachments: attachments,
            PreviousRequests: previousRequests
        );
    }
}
