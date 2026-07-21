using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductReviews;

/// <summary>Full admin view of a product review, including moderation metadata.</summary>
public class ProductReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    /// <summary>Reviewed product's name (populated when the product is eager-loaded; otherwise null).</summary>
    public string? ProductName { get; set; }
    public Guid CustomerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ModeratedOnUtc { get; set; }
    public Guid? ModeratedById { get; set; }

    public static ProductReviewDto From(ProductReview r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductName = r.Product?.Name,
        CustomerId = r.CustomerId,
        ReviewerName = r.ReviewerName,
        Rating = r.Rating,
        Title = r.Title,
        Body = r.Body,
        Status = r.Status.ToString(),
        CreatedOnUtc = r.CreatedOnUtc,
        ModeratedOnUtc = r.ModeratedOnUtc,
        ModeratedById = r.ModeratedById,
    };
}

/// <summary>Public (storefront) view of an approved review — no customer id or moderation internals.</summary>
public class ProductReviewPublicDto
{
    public Guid Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }

    public static ProductReviewPublicDto From(ProductReview r) => new()
    {
        Id = r.Id,
        ReviewerName = r.ReviewerName,
        Rating = r.Rating,
        Title = r.Title,
        Body = r.Body,
        CreatedOnUtc = r.CreatedOnUtc,
    };
}

/// <summary>Aggregate rating summary for a product's storefront reviews section. When
/// <see cref="Enabled"/> is false the whole section is hidden and every count is zero.</summary>
public class ProductReviewSummaryDto
{
    public bool Enabled { get; set; }
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
    public int FiveStar { get; set; }
    public int FourStar { get; set; }
    public int ThreeStar { get; set; }
    public int TwoStar { get; set; }
    public int OneStar { get; set; }

    /// <summary>Reviews are turned off for this product — the storefront hides the section entirely.</summary>
    public static ProductReviewSummaryDto Disabled() => new() { Enabled = false };

    /// <summary>Builds the summary from a product's already-materialised approved reviews.</summary>
    public static ProductReviewSummaryDto FromApproved(IReadOnlyCollection<ProductReview> approved) => new()
    {
        Enabled = true,
        TotalCount = approved.Count,
        AverageRating = approved.Count == 0 ? 0 : Math.Round(approved.Average(r => r.Rating), 2),
        FiveStar = approved.Count(r => r.Rating == 5),
        FourStar = approved.Count(r => r.Rating == 4),
        ThreeStar = approved.Count(r => r.Rating == 3),
        TwoStar = approved.Count(r => r.Rating == 2),
        OneStar = approved.Count(r => r.Rating == 1),
    };
}

/// <summary>The public reviews payload for a product page: the aggregate summary plus the approved reviews (newest first).</summary>
public class ProductReviewListResultDto
{
    public ProductReviewSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<ProductReviewPublicDto> Reviews { get; set; } = Array.Empty<ProductReviewPublicDto>();
}

/// <summary>Lightweight moderation-queue counts for the admin reviews dashboard.</summary>
public class ProductReviewStatsDto
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TotalCount { get; set; }
}
