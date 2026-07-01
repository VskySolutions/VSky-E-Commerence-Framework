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
