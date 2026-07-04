using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAuth;

/// <summary>
/// Registers a new customer (identity + profile) and issues a single-use email-verification token,
/// then enqueues the verification email (REQ-CUS-001, AC-CUS-001.1/.2).
/// </summary>
public record RegisterCustomerCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? RecaptchaToken = null) : IRequest<Guid>;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).MaximumLength(200);
    }
}

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailEnqueuer _emails;
    private readonly IRecaptchaVerifier _recaptcha;
    private readonly IStorefrontUrlBuilder _urls;

    public RegisterCustomerCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        IDateTimeProvider clock,
        IEmailEnqueuer emails,
        IRecaptchaVerifier recaptcha,
        IStorefrontUrlBuilder urls)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _emails = emails;
        _recaptcha = recaptcha;
        _urls = urls;
    }

    public async Task<Guid> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        await _recaptcha.VerifyOrThrowAsync(RecaptchaFormType.Register, request.RecaptchaToken, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException($"A user with email '{email}' already exists.");

        var username = await GenerateUniqueUsernameAsync(email, cancellationToken);
        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            EmailVerified = false,
            IsActive = true,
            Customer = new Customer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
            },
        };

        // Persist identity + profile in one transaction (AC-CUS-001.2).
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        // Issue a single-use, 24-hour verification token; only its hash is stored.
        var rawToken = _tokens.GenerateRefreshToken();
        _db.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Purpose = UserTokenPurpose.EmailVerification,
            TokenHash = _tokens.HashRefreshToken(rawToken),
            CreatedOnUtc = _clock.UtcNow,
            ExpiresOnUtc = _clock.UtcNow.AddHours(24),
        });
        await _db.SaveChangesAsync(cancellationToken);

        var verifyUrl = _urls.EmailVerificationUrl(rawToken);
        var body =
            $"Hi {fullName},\n\n" +
            "Thanks for creating an account. Please confirm your email address by opening the link below:\n\n" +
            $"{verifyUrl}\n\n" +
            "This link expires in 24 hours. If you did not create this account, you can ignore this email.";

        await _emails.EnqueueAsync(
            "CustomerEmailVerification",
            user.Email,
            fullName,
            "Verify your email",
            body,
            NotificationCategory.Transactional,
            cancellationToken);

        return user.Id;
    }

    /// <summary>
    /// Derives a username from the email local-part, suffixing an incrementing number
    /// (e.g. jane, jane1, jane2, …) until an unused value is found.
    /// </summary>
    private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken cancellationToken)
    {
        var baseName = email.Split('@')[0];
        var candidate = baseName;
        var suffix = 1;
        while (await _db.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
            candidate = $"{baseName}{suffix++}";
        return candidate;
    }
}
