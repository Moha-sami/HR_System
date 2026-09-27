using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.DeleteRequestType;

public class DeleteRequestTypeCommandHandler : IRequestHandler<DeleteRequestTypeCommand, bool>
{
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IRepository<Request> _requestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRequestTypeCommandHandler(
        IRepository<RequestType> requestTypeRepository,
        IRepository<Request> requestRepository,
        IUnitOfWork unitOfWork)
    {
        _requestTypeRepository = requestTypeRepository;
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRequestTypeCommand request, CancellationToken cancellationToken)
    {
        var requestType = await _requestTypeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (requestType == null)
        {
            throw new KeyNotFoundException($"Request type with ID {request.Id} was not found.");
        }

        var isReferenced = await _requestRepository.AnyAsync(r => r.RequestTypeId == request.Id, cancellationToken);
        if (isReferenced)
        {
            throw new InvalidOperationException("Cannot delete request type because it is referenced by existing employee requests.");
        }

        _requestTypeRepository.Delete(requestType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
