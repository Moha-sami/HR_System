using Buy2.Application.DTOs.Recognitions;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace Buy2.Application.Features.Recognitions.Commands.UpdateRecognition;

public record UpdateRecognitionCommand(
    int Id,
    int RecipientId,
    string Title,
    string Narrative,
    int AwardedPoints,
    string? Badge = null,
    string? Status = null,
    DateTime? ScheduledFor = null,
    int? ModifyingUserId = null,
    bool IsElevatedUser = false,
    IFormFile? AttachmentFile = null
) : IRequest<RecognitionResponseDto>;
