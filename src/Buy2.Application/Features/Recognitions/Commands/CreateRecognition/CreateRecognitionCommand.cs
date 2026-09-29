using Buy2.Application.DTOs.Recognitions;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace Buy2.Application.Features.Recognitions.Commands.CreateRecognition;

public record CreateRecognitionCommand(
    int RecipientId,
    string Title,
    string Narrative,
    int AwardedPoints = 0,
    string? Badge = null,
    string Status = "Published",
    DateTime? ScheduledFor = null,
    int? AuthorId = null,
    IFormFile? AttachmentFile = null
) : IRequest<RecognitionResponseDto>;
