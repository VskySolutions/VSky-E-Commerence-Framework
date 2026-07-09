using Microsoft.Extensions.Configuration;
using VSky.Application.Common.Interfaces;

namespace VSky.Infrastructure.Storefront;

/// <summary>
/// Resolves the public storefront base URL from configuration ("Storefront:PublicBaseUrl") and builds the
/// customer-facing verification/reset links against the SPA's <c>/shop</c> account routes. Falls back to the
/// dev SPA origin when unset.
/// </summary>
public class StorefrontUrlBuilder : IStorefrontUrlBuilder
{
    private readonly string _baseUrl;

    public StorefrontUrlBuilder(IConfiguration configuration)
    {
        var configured = configuration["Storefront:PublicBaseUrl"];
        _baseUrl = string.IsNullOrWhiteSpace(configured) ? "http://localhost:9000" : configured.TrimEnd('/');
    }

    public string EmailVerificationUrl(string token) => $"{_baseUrl}/shop/verify-email?token={token}";

    public string PasswordResetUrl(string token) => $"{_baseUrl}/shop/reset-password?token={token}";

    public string AdminPasswordResetUrl(string token) => $"{_baseUrl}/auth/reset-password?token={token}";
}
