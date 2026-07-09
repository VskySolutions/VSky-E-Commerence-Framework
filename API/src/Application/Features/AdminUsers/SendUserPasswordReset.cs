using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.AdminUsers;

/// <summary>
/// Emails the given user a single-use password-reset link (admin-triggered). Unlike the public
/// forgot-password flow this reports a missing user (the admin picked a known account), and the
/// link targets the admin SPA's <c>/auth/reset-password</c> route.
/// </summary>
public record SendUserPasswordResetCommand(Guid Id) : IRequest;

public class SendUserPasswordResetCommandHandler : IRequestHandler<SendUserPasswordResetCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailEnqueuer _emails;
    private readonly IStorefrontUrlBuilder _urls;

    public SendUserPasswordResetCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService tokens,
        IDateTimeProvider clock,
        IEmailEnqueuer emails,
        IStorefrontUrlBuilder urls)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
        _emails = emails;
        _urls = urls;
    }

    public async Task Handle(SendUserPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

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
        var resetUrl = _urls.AdminPasswordResetUrl(rawToken);
        var body =
            $"Hi {greeting},\n\n" +
            "An administrator asked us to help you set a new password. Open the link below to choose one:\n\n" +
            $"{resetUrl}\n\n" +
            "This link expires in 1 hour. If you did not expect this, please contact your administrator.";

        await _emails.EnqueueAsync(
            "AdminPasswordReset",
            user.Email,
            string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            "Reset your password",
            body,
            NotificationCategory.Transactional,
            cancellationToken);
    }
}
