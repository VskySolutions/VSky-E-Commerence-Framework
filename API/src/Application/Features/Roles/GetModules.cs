using MediatR;
using VSky.Application.Common.Authorization;

namespace VSky.Application.Features.Roles;

/// <summary>Returns the catalog of admin modules so an admin UI can populate the role editor.</summary>
public record GetModulesQuery : IRequest<IReadOnlyList<ModuleInfo>>;

public class GetModulesQueryHandler : IRequestHandler<GetModulesQuery, IReadOnlyList<ModuleInfo>>
{
    public Task<IReadOnlyList<ModuleInfo>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Modules.All);
}
