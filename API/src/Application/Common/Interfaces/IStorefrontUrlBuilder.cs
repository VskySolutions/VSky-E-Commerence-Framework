namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Builds public storefront links for outbound customer emails (verification, password reset) from
/// configuration, so links resolve correctly in every environment instead of a hard-coded localhost URL.
/// </summary>
public interface IStorefrontUrlBuilder
{
    string EmailVerificationUrl(string token);
    string PasswordResetUrl(string token);
}
