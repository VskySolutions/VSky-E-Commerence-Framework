using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Roles;

public record GetRoleQuery(Guid Id) : IRequest<RoleDto>;

public class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, RoleDto>
{
    private readonly IApplicationDbContext _db;

    public GetRoleQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<RoleDto> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Role", request.Id);

        return RoleDto.From(role);
    }
}
