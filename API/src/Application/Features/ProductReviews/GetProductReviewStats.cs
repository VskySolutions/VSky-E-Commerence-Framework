using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.ProductReviews;

/// <summary>Moderation-queue counts for the admin reviews dashboard (WO-14), optionally scoped to one product.</summary>
public record GetProductReviewStatsQuery(Guid? ProductId = null) : IRequest<ProductReviewStatsDto>;

public class GetProductReviewStatsQueryHandler : IRequestHandler<GetProductReviewStatsQuery, ProductReviewStatsDto>
{
    private readonly IApplicationDbContext _db;

    public GetProductReviewStatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductReviewStatsDto> Handle(GetProductReviewStatsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProductReview> query = _db.ProductReviews.AsNoTracking();

        if (request.ProductId is Guid productId)
            query = query.Where(r => r.ProductId == productId);

        var counts = await query
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(ReviewStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        return new ProductReviewStatsDto
        {
            PendingCount = CountOf(ReviewStatus.Pending),
            ApprovedCount = CountOf(ReviewStatus.Approved),
            RejectedCount = CountOf(ReviewStatus.Rejected),
            TotalCount = counts.Sum(c => c.Count),
        };
    }
}
