using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>A database-backed content page (Privacy Policy, About Us, FAQ, ...) with SEO metadata and a
/// publish lifecycle (WO-54). CMS-owned: table name is <c>CMSPages</c>.</summary>
public class CMSPage : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Body { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }

    public CmsContentStatus Status { get; set; } = CmsContentStatus.Draft;

    /// <summary>Optional organising group (drives footer/nav link columns).</summary>
    public Guid? PageGroupId { get; set; }
    public CMSPageGroup? PageGroup { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Pre-seeded informational pages are flagged so the UI can protect them from deletion.</summary>
    public bool IsSystemPage { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }
}
