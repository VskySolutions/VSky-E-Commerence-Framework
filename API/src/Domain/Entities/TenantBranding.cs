using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// Deployment branding consumed by the Client App at startup and applied as CSS custom properties
/// before first render (Tenant Management blueprint). Effectively a singleton per deployment.
/// </summary>
public class TenantBranding : AuditableEntity
{
    public string BrandName { get; set; } = string.Empty;
    public string? Domain { get; set; }

    /// <summary>Central Media assets for the logo/favicon (preferred). Uploads set these going forward.</summary>
    public Guid? LogoMediaId { get; set; }
    public Media? LogoMedia { get; set; }
    public Guid? FaviconMediaId { get; set; }
    public Media? FaviconMedia { get; set; }

    /// <summary>Legacy logo/favicon URLs, retained as read fallbacks for pre-migration records.</summary>
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
}
