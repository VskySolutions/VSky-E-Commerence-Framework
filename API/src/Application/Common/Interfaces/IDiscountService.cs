using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Evaluates the configured discount rules against a cart (REQ-PRP-001). Non-exclusive discounts
/// stack; an applicable exclusive discount overrides all others, the single most-favorable one
/// winning (AC-PRP-001.4). Returns an itemized breakdown so callers can show buyers exactly which
/// promotions applied.
/// </summary>
public interface IDiscountService
{
    /// <summary>
    /// Computes the discounts that apply to the given cart lines and subtotal and their total
    /// reduction. Only active, in-window discounts whose minimum-order threshold is met are considered.
    /// </summary>
    Task<DiscountEvaluationResult> EvaluateAsync(
        IReadOnlyList<DiscountCartLine> lines, decimal subtotal, CancellationToken ct = default);
}
