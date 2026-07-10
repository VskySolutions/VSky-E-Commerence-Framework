using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Branding;

/// <summary>Creates or updates the deployment's singleton branding row.</summary>
public record UpdateBrandingCommand(
    string BrandName,
    string? Domain,
    Guid? LogoMediaId,
    Guid? FaviconMediaId,
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string? FontFamily,
    string? SupportEmail,
    string? SupportPhone,
    string? SocialLinksJson,
    string? LayoutOptionsJson,
    string? DefaultLanguage,
    string? DisplayTimeZone = null) : IRequest<BrandingDto>;

public class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Domain).MaximumLength(255);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.FaviconUrl).MaximumLength(500);
        RuleFor(x => x.PrimaryColor).MaximumLength(32);
        RuleFor(x => x.SecondaryColor).MaximumLength(32);
        RuleFor(x => x.AccentColor).MaximumLength(32);
    }
}

public class UpdateBrandingCommandHandler : IRequestHandler<UpdateBrandingCommand, BrandingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPublisher _publisher;

    public UpdateBrandingCommandHandler(IApplicationDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<BrandingDto> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TenantBrandings.FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            entity = new TenantBranding();
            _db.TenantBrandings.Add(entity);
        }

        entity.BrandName = request.BrandName;
        entity.Domain = request.Domain;
        entity.LogoMediaId = request.LogoMediaId;
        entity.FaviconMediaId = request.FaviconMediaId;
        // Going forward the assets live in Media; clear the legacy URL once a Media asset is chosen.
        entity.LogoUrl = request.LogoMediaId.HasValue ? null : request.LogoUrl;
        entity.FaviconUrl = request.FaviconMediaId.HasValue ? null : request.FaviconUrl;
        entity.PrimaryColor = request.PrimaryColor;
        entity.SecondaryColor = request.SecondaryColor;
        entity.AccentColor = request.AccentColor;
        entity.FontFamily = request.FontFamily;
        entity.SupportEmail = request.SupportEmail;
        entity.SupportPhone = request.SupportPhone;
        entity.SocialLinksJson = request.SocialLinksJson;
        entity.LayoutOptionsJson = request.LayoutOptionsJson;
        entity.DefaultLanguage = request.DefaultLanguage;
        entity.DisplayTimeZone = string.IsNullOrWhiteSpace(request.DisplayTimeZone) ? "UTC" : request.DisplayTimeZone.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TenantBrandingUpdated(entity.Id, entity.BrandName), cancellationToken);
        return BrandingDto.From(entity);
    }
}
