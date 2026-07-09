using VSky.Domain.Entities;

namespace VSky.Application.Features.Branding;

/// <summary>Deployment branding consumed by the Client App at startup and applied before first render.</summary>
public class BrandingDto
{
    public string BrandName { get; set; } = string.Empty;
    public string? Domain { get; set; }

    /// <summary>Central Media asset ids for the logo/favicon (preferred going forward; null for un-migrated records).</summary>
    public Guid? LogoMediaId { get; set; }
    public Guid? FaviconMediaId { get; set; }

    /// <summary>Resolved logo/favicon URLs: the Media asset's URL when present, else the legacy fallback column.</summary>
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? FontFamily { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? SocialLinksJson { get; set; }
    public string? LayoutOptionsJson { get; set; }
    public string? DefaultLanguage { get; set; }

    public static BrandingDto From(TenantBranding b) => new()
    {
        BrandName = b.BrandName,
        Domain = b.Domain,
        LogoMediaId = b.LogoMediaId,
        FaviconMediaId = b.FaviconMediaId,
        LogoUrl = b.LogoMedia?.Url ?? b.LogoUrl,
        FaviconUrl = b.FaviconMedia?.Url ?? b.FaviconUrl,
        PrimaryColor = b.PrimaryColor,
        SecondaryColor = b.SecondaryColor,
        AccentColor = b.AccentColor,
        FontFamily = b.FontFamily,
        SupportEmail = b.SupportEmail,
        SupportPhone = b.SupportPhone,
        SocialLinksJson = b.SocialLinksJson,
        LayoutOptionsJson = b.LayoutOptionsJson,
        DefaultLanguage = b.DefaultLanguage,
    };
}
