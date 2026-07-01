using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>Performs a lightweight connectivity probe for a stored credential before activation.</summary>
public interface ICredentialConnectivityChecker
{
    Task<ConnectivityTestResult> TestAsync(string serviceType, string plaintextValue, CancellationToken cancellationToken = default);
}
