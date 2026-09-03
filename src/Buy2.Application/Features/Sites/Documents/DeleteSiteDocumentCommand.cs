using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.Documents;

public record DeleteSiteDocumentCommand(int SiteId, int DocumentId) : IRequest;
public class DeleteSiteDocumentCommandHandler : IRequestHandler<DeleteSiteDocumentCommand>
{
    private readonly IRepository<SiteDocument> _siteDocumentRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteSiteDocumentCommandHandler(IRepository<SiteDocument> siteDocumentRepository, IUnitOfWork unitOfWork)
    {
        _siteDocumentRepository = siteDocumentRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task Handle(DeleteSiteDocumentCommand command, CancellationToken cancellation)
    {

        var document = await _siteDocumentRepository
            .Query()
            .FirstOrDefaultAsync(sd => sd.Id == command.DocumentId &&
                                sd.SiteId == command.SiteId, cancellation);

        if (document is null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        if (File.Exists(document.FilePath))
        {
            File.Delete(document.FilePath);
        }

        _siteDocumentRepository.Delete(document);
        await _unitOfWork.SaveChangesAsync(cancellation);
    }
}