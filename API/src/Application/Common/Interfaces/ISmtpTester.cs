using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>Sends a test email to verify an SMTP account's configuration.</summary>
public interface ISmtpTester
{
    Task<ConnectivityTestResult> TestSendAsync(SmtpTestRequest request, CancellationToken cancellationToken = default);
}
