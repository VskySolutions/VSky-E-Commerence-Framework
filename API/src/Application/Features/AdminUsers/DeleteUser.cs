using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.AdminUsers;

/// <summary>Soft-deletes a user (idempotent). SuperAdmin accounts are protected and can never be deleted.</summary>
public record DeleteUserCommand(Guid Id) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteUserCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            return;

        // SuperAdmin accounts are protected — they can never be deleted (not just the last one).
        var superAdmin = nameof(RoleType.SuperAdmin);
        var isSuperAdmin = user.UserRoles.Any(ur => ur.Role is not null && ur.Role.Name == superAdmin);
        if (isSuperAdmin)
            throw new ConflictException("SuperAdmin accounts cannot be deleted.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
