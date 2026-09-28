using Buy2.Application.DTOs.News;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace Buy2.Application.Features.News.CreateNewsPost;

public record CreateNewsPostCommand(
    string Title,
    string Content,
    string Category = "General",
    string Status = "Draft",
    DateTime? ScheduledFor = null,
    int? AuthorId = null,
    IFormFile? MediaFile = null
) : IRequest<NewsPostResponseDto>;
