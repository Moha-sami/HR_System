using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.GetNewsFeed;

public class GetNewsFeedQueryHandler : IRequestHandler<GetNewsFeedQuery, PaginatedListResult<NewsFeedSummaryDto>>
{
    private readonly IRepository<Post> _postRepository;

    public GetNewsFeedQueryHandler(IRepository<Post> postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<PaginatedListResult<NewsFeedSummaryDto>> Handle(
        GetNewsFeedQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _postRepository.Query(asNoTracking: true)
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Reactions)
            .Where(p => p.PostType == "News")
            .AsQueryable();

        // 1. Role-based visibility enforcement
        if (!request.IsElevatedUser)
        {
            // Non-elevated employees only see published posts or scheduled posts whose release time arrived
            query = query.Where(p => p.Status == NewsLifecycleManager.StatusPublished ||
                                     (p.Status == NewsLifecycleManager.StatusScheduled && p.ScheduledFor.HasValue && p.ScheduledFor.Value <= now));
        }
        else if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var filterStatus = request.Status.Trim();
            query = query.Where(p => p.Status == filterStatus);
        }

        // 2. Keyword search matching Title or Content
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
        }

        // 3. Category filter
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(p => p.Category == category);
        }

        // 4. Sorting: published timestamp / creation timestamp descending
        query = query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        // 5. Pagination
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var rawItems = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.Category,
                p.Status,
                p.AuthorId,
                AuthorName = p.Author != null ? (p.Author.FirstName + " " + p.Author.LastName).Trim() : "Corporate Admin",
                p.MediaUrl,
                p.ScheduledFor,
                p.PublishedAt,
                LikesCount = p.Reactions.Count,
                CommentsCount = p.Comments.Count(c => !c.IsDeleted),
                p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(p =>
        {
            var summary = p.Content.Length > 160 ? p.Content.Substring(0, 160) + "..." : p.Content;
            var effectiveStatus = NewsLifecycleManager.ResolveEffectiveStatus(p.Status, p.ScheduledFor);

            return new NewsFeedSummaryDto(
                p.Id,
                p.Title,
                summary,
                p.Category,
                effectiveStatus,
                p.AuthorId,
                p.AuthorName,
                null,
                p.MediaUrl,
                p.ScheduledFor,
                p.PublishedAt,
                p.LikesCount,
                p.CommentsCount,
                p.CreatedAt
            );
        }).ToList();

        return new PaginatedListResult<NewsFeedSummaryDto>(
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}
