using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAuth;

/// <summary>
/// Starts the password-reset flow. When an active account matches the email, a single-use reset
/// token is created and a reset email enqueued. Always completes without error — even for unknown
/// accounts — so callers cannot probe for registered emails (AC-CUS-001.6).
/// </summary>
public record RequestPasswordResetCommand(string Email, string? RecaptchaToken = null) : IRequest;

public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailEnqueuer _emails;
    private readonly IRecaptchaVerifier _recaptcha;
    private readonly IStorefrontUrlBuilder _urls;

    public RequestPasswordResetCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService tokens,
        IDateTimeProvider clock,
        IEmailEnqueuer emails,
        IRecaptchaVerifier recaptcha,
        IStorefrontUrlBuilder urls)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
        _emails = emails;
        _recaptcha = recaptcha;
        _urls = urls;
    }

    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        await _recaptcha.VerifyOrThrowAsync(RecaptchaFormType.PasswordReset, request.RecaptchaToken, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

        // Silently no-op for unknown/inactive accounts to avoid disclosing whether the email exists.
        if (user is null)
            return;

        // Issue a single-use, 1-hour reset token; only its hash is stored.
        var rawToken = _tokens.GenerateRefreshToken();
        _db.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Purpose = UserTokenPurpose.PasswordReset,
            TokenHash = _tokens.HashRefreshToken(rawToken),
            CreatedOnUtc = _clock.UtcNow,
            ExpiresOnUtc = _clock.UtcNow.AddHours(1),
        });
        await _db.SaveChangesAsync(cancellationToken);

        var fullName = user.Customer is null
            ? string.Empty
            : $"{user.Customer.FirstName} {user.Customer.LastName}".Trim();
        var greeting = string.IsNullOrWhiteSpace(fullName) ? "there" : fullName;
        var resetUrl = _urls.PasswordResetUrl(rawToken);
        var body =
            $"Hi {greeting},\n\n" +
            "We received a request to reset your password. Open the link below to choose a new one:\n\n" +
            $"{resetUrl}\n\n" +
            "This link expires in 1 hour. If you did not request this, you can safely ignore this email.";

        await _emails.EnqueueAsync(
            "CustomerPasswordReset",
            user.Email,
            string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            "Reset your password",
            body,
            NotificationCategory.Transactional,
            cancellationToken);
    }
}
