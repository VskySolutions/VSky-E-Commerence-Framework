using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Email;

/// <summary>
/// Persists a pre-rendered <see cref="EmailQueue"/> row in <see cref="EmailStatus.Pending"/> state
/// for the email dispatch worker to send asynchronously (WO-20).
/// </summary>
public class EmailEnqueuer : IEmailEnqueuer
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public EmailEnqueuer(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EnqueueAsync(
        string templateKey,
        string recipientEmail,
        string? recipientName,
        string subject,
        string body,
        NotificationCategory category = NotificationCategory.Transactional,
        CancellationToken cancellationToken = default)
    {
        _db.EmailQueue.Add(new EmailQueue
        {
            TemplateKey = templateKey,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            RenderedSubject = subject,
            RenderedBody = body,
            Category = category,
            Status = EmailStatus.Pending,
            CreatedOnUtc = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
