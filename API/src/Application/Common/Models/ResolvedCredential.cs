namespace VSky.Application.Common.Models;

/// <summary>
/// An active integration credential resolved for runtime use: its raw string/JSON value (the same value
/// <see cref="Interfaces.ICredentialVault.GetCredentialAsync"/> returns), whether the row is a production
/// (live) credential, and the API host it belongs to.
/// </summary>
/// <param name="BaseUrl">
/// The endpoint this credential authenticates against, straight from its admin-configured column. Adapters
/// hold no fallback: sandbox and live are different hosts for most providers, and a constant compiled into
/// the adapter cannot know which account an admin pasted in — guessing sends sandbox keys to the live host
/// and fails with an opaque 4xx. Null only for providers with genuinely no endpoint to choose (Stripe, whose
/// SDK owns the host and whose key prefix decides the mode).
///
/// The shipping carriers also see this inside their JSON <see cref="Value"/>, which already models it as
/// part of each adapter's credential contract; this envelope is how the providers whose value is a bare
/// secret (PayPal, Square, …) receive it.
/// </param>
public sealed record ResolvedCredential(string Value, bool IsProduction, string? BaseUrl = null);
