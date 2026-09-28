using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.DeleteNewsPost;

public class DeleteNewsPostCommandHandler : IRequestHandler<DeleteNewsPostCommand, bool>
{
    private readonly IRepository<Post> _postRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNewsPostCommandHandler(
        IRepository<Post> postRepository,
        IUnitOfWork unitOfWork)
    {
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteNewsPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.Query()
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.PostType == "News", cancellationToken);

        if (post == null)
        {
            throw new KeyNotFoundException($"News post with ID {request.Id} was not found.");
        }

        // AC 1: Verifies that news post exists and user has administrative authority
        if (!request.IsElevatedUser)
        {
            throw new UnauthorizedAccessException("User does not have administrative authority to delete news posts.");
        }

        var now = DateTime.UtcNow;

        // AC 2: Applies soft deletion flag to post record, hiding it from all standard listings and feeds
        post.IsDeleted = true;
        post.DeletedAt = now;

        // AC 3: Suppresses associated comments from public visibility while maintaining data auditability
        foreach (var comment in post.Comments.Where(c => !c.IsDeleted))
        {
            comment.IsDeleted = true;
            comment.DeletedAt = now;
        }

        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
