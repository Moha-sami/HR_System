using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Sites.Documents;

public record UploadSiteDocumentCommand(int SiteId, IFormFile File) : IRequest<DocumentDto>;

public class UploadSiteDocumentCommandHandler : IRequestHandler<UploadSiteDocumentCommand, DocumentDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<SiteDocument> _siteDocumentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadSiteDocumentCommandHandler(IRepository<Site> siteRepository, IRepository<SiteDocument> siteDocumentRepository, IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _siteDocumentRepository = siteDocumentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DocumentDto> Handle(UploadSiteDocumentCommand command, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .Query(false)
            .AnyAsync(s => s.Id == command.SiteId, cancellation);
        if (!site)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        if (command.File is null || command.File.Length == 0)
        {
            throw new ValidationException("File not found.");
        }

        // 1 MB == 1.048.576 Bytes
        const long maxSize = 10 * 1024 * 1024;

        if (command.File.Length > maxSize)
        {
            throw new ValidationException("File size exceeds the maximum allowed size of 10 MB.");
        }

        var extension = Path.GetExtension(command.File.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[]
         {
            ".pdf",
            ".doc",
            ".docx"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ValidationException("Invalid file type. Only PDF and DOC files are allowed.");
        }


        // Create Unique File Name
        var fileName = $"{Guid.NewGuid()}{extension}";

        // Create Storage Directory
        var directoryUpload = Path.Combine
            (
                Directory.GetCurrentDirectory(), "Storage", "Uploads"
            );

        Directory.CreateDirectory(directoryUpload);

        // Physical Path
        var filePath = Path.Combine
            (
                 directoryUpload, fileName
            );

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await command.File.CopyToAsync(fileStream, cancellation);
        }

        var document = new SiteDocument
        {
            SiteId = command.SiteId,
            FileName = command.File.FileName,
            FilePath = filePath,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await _siteDocumentRepository.AddAsync(document);
        await _unitOfWork.SaveChangesAsync(cancellation);

        var url = $"/api/v1/sites/{command.SiteId}/documents/{document.Id}";

        return new DocumentDto(document.Id, document.FileName, url);
    }
}