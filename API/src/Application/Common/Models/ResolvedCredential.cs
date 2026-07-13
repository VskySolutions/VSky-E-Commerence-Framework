namespace VSky.Application.Common.Models;

/// <summary>
/// An active integration credential resolved for runtime use: its raw string/JSON value (the same value
/// <see cref="Interfaces.ICredentialVault.GetCredentialAsync"/> returns) plus whether the row is a
/// production (live) credential — so environment-aware adapters can select the sandbox vs. live endpoint.
/// </summary>
public sealed record ResolvedCredential(string Value, bool IsProduction);
