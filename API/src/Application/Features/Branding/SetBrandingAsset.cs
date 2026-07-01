using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Branding;

/// <summary>Persists an uploaded logo or favicon public URL onto the deployment's singleton branding row.</summary>
public record SetBrandingAssetCommand(bool IsLogo, string Url) : IRequest<BrandingDto>;

public class SetBrandingAssetCommandHandler : IRequestHandler<SetBrandingAssetCommand, BrandingDto>
{
    private readonly IApplicationDbContext _db;

    public SetBrandingAssetCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BrandingDto> Handle(SetBrandingAssetCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TenantBrandings.FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            entity = new TenantBranding();
            _db.TenantBrandings.Add(entity);
        }

        if (request.IsLogo)
            entity.LogoUrl = request.Url;
        else
            entity.FaviconUrl = request.Url;

        await _db.SaveChangesAsync(cancellationToken);
        return BrandingDto.From(entity);
    }
}
