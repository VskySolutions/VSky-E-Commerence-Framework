using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Resolves the configured tax provider and returns a tax breakdown for a request (REQ-TAX-001).
/// Caches identical origin/destination/line results for the configured TTL (AC-TAX-001.5) and applies
/// a flat-rate fallback — raising an admin alert — when the active provider is unavailable
/// (AC-TAX-001.4 / AC-TAX-002.3).
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    /// Calculates tax for the request. Never throws for provider failures: on error it returns a
    /// flat-rate <see cref="TaxBreakdown"/> with <see cref="TaxBreakdown.FallbackApplied"/> set.
    /// </summary>
    Task<TaxBreakdown> CalculateAsync(TaxCalculationRequest request, CancellationToken cancellationToken = default);
}
