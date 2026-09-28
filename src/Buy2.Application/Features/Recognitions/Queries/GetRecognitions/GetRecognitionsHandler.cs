using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitions;

public class GetRecognitionsHandler : IRequestHandler<GetRecognitionsQuery, PaginatedListResult<RecognitionSummaryDto>>
{
    private readonly IRepository<Recognition> _recognitionRepository;

    public GetRecognitionsHandler(IRepository<Recognition> recognitionRepository)
    {
        _recognitionRepository = recognitionRepository;
    }

    public async Task<PaginatedListResult<RecognitionSummaryDto>> Handle(
        GetRecognitionsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _recognitionRepository.Query(asNoTracking: true)
            .Include(r => r.Author)
            .Include(r => r.Recipient)
            .Include(r => r.Reactions)
            .AsQueryable();

        // 1. Role-based visibility enforcement
        if (!request.IsElevatedUser)
        {
            query = query.Where(r => r.Status == RecognitionLifecycleManager.StatusPublished ||
                                     (r.Status == RecognitionLifecycleManager.StatusScheduled && r.ScheduledFor.HasValue && r.ScheduledFor.Value <= now));
        }
        else if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var filterStatus = request.Status.Trim();
            if (string.Equals(filterStatus, "Drafted", StringComparison.OrdinalIgnoreCase))
            {
                filterStatus = RecognitionLifecycleManager.StatusDraft;
            }

            query = query.Where(r => r.Status == filterStatus);
        }

        // 2. Keyword search matching Title, Narrative, Recipient name, or Author name
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(r => r.Title.Contains(search) ||
                                     r.Narrative.Contains(search) ||
                                     (r.Recipient != null && (r.Recipient.FirstName.Contains(search) || r.Recipient.LastName.Contains(search))) ||
                                     (r.Author != null && (r.Author.FirstName.Contains(search) || r.Author.LastName.Contains(search))));
        }

        // 3. Dynamic multi-column ordering
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var isAsc = string.Equals(request.SortDirection?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);

        query = sortBy switch
        {
            "points" or "awardedpoints" => isAsc
                ? query.OrderBy(r => r.AwardedPoints)
                : query.OrderByDescending(r => r.AwardedPoints),

            "title" => isAsc
                ? query.OrderBy(r => r.Title)
                : query.OrderByDescending(r => r.Title),

            "employee" or "recipient" or "recipientname" => isAsc
                ? query.OrderBy(r => r.Recipient != null ? r.Recipient.FirstName : "").ThenBy(r => r.Recipient != null ? r.Recipient.LastName : "")
                : query.OrderByDescending(r => r.Recipient != null ? r.Recipient.FirstName : "").ThenByDescending(r => r.Recipient != null ? r.Recipient.LastName : ""),

            "author" or "authorname" => isAsc
                ? query.OrderBy(r => r.Author != null ? r.Author.FirstName : "").ThenBy(r => r.Author != null ? r.Author.LastName : "")
                : query.OrderByDescending(r => r.Author != null ? r.Author.FirstName : "").ThenByDescending(r => r.Author != null ? r.Author.LastName : ""),

            "date" or "publishedat" or "datetime" => isAsc
                ? query.OrderBy(r => r.PublishedAt ?? r.CreatedAt)
                : query.OrderByDescending(r => r.PublishedAt ?? r.CreatedAt),

            _ => isAsc
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt)
        };

        // 4. Pagination
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedEntities = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pagedEntities.Select(r =>
        {
            var authorName = r.Author != null ? $"{r.Author.FirstName} {r.Author.LastName}".Trim() : "Corporate HR";
            var recipientName = r.Recipient != null ? $"{r.Recipient.FirstName} {r.Recipient.LastName}".Trim() : "Colleague";
            var effectiveStatus = RecognitionLifecycleManager.ResolveEffectiveStatus(r.Status, r.ScheduledFor);
            var likesCount = r.Reactions?.Count(react => react.ReactionType == "Like") ?? 0;

            return new RecognitionSummaryDto(
                Id: r.Id,
                Title: r.Title,
                Narrative: r.Narrative,
                Badge: r.Badge,
                Status: effectiveStatus,
                AuthorId: r.AuthorId,
                AuthorName: authorName,
                RecipientId: r.RecipientId,
                RecipientName: recipientName,
                RecipientAvatar: r.Recipient?.ProfilePhotoUrl,
                AwardedPoints: r.AwardedPoints,
                ScheduledFor: r.ScheduledFor,
                PublishedAt: r.PublishedAt,
                LikesCount: likesCount,
                CreatedAt: r.CreatedAt,
                UpdatedAt: r.UpdatedAt
            );
        }).ToList();

        return new PaginatedListResult<RecognitionSummaryDto>(
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}
