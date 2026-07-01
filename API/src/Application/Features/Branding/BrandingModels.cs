using VSky.Domain.Entities;

namespace VSky.Application.Features.Branding;

/// <summary>Deployment branding consumed by the Client App at startup and applied before first render.</summary>
public class BrandingDto
{
    public string BrandName { get; set; } = string.Empty;
    public string? Domain { get; set; }
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
        LogoUrl = b.LogoUrl,
        FaviconUrl = b.FaviconUrl,
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
