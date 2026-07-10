using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.EmailTemplates;

namespace VSky.Infrastructure.Email;

/// <summary>
/// Default <see cref="IEmailTemplateSender"/>: loads the stored template by key, merges tenant branding
/// tokens with the caller's variables, renders the subject + HTML body with the shared token engine, and
/// enqueues the result as an HTML email. Disabled or missing templates are skipped (nothing is sent).
/// </summary>
public class EmailTemplateSender : IEmailTemplateSender
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailEnqueuer _emails;

    public EmailTemplateSender(IApplicationDbContext db, IEmailEnqueuer emails)
    {
        _db = db;
        _emails = emails;
    }

    public async Task<bool> SendAsync(
        string templateKey,
        string recipientEmail,
        string? recipientName,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return false;

        var template = await _db.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey, cancellationToken);
        if (template is null || !template.Enabled)
            return false;

        var branding = await _db.TenantBrandings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        // Branding tokens the shared HTML shell references; caller variables override/extend them.
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["brandName"] = branding?.BrandName ?? string.Empty,
            ["primaryColor"] = string.IsNullOrWhiteSpace(branding?.PrimaryColor) ? "#111827" : branding!.PrimaryColor!,
            ["footerAddress"] = string.Empty,
            ["unsubscribeBlock"] = string.Empty,
        };
        foreach (var kv in variables)
            values[kv.Key] = kv.Value ?? string.Empty;

        var subject = PreviewEmailTemplateQueryHandler.Render(template.SubjectLine, values);
        var body = PreviewEmailTemplateQueryHandler.Render(template.HtmlBody, values);

        await _emails.EnqueueAsync(
            templateKey,
            recipientEmail,
            recipientName,
            subject,
            body,
            template.Category,
            isHtml: true,
            cancellationToken: cancellationToken);

        return true;
    }
}
