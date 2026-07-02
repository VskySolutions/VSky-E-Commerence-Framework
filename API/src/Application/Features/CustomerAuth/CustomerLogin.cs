using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.Authentication;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAuth;

/// <summary>
/// Authenticates a customer and issues an access + refresh token pair. Requires a verified email
/// and gives a generic error on bad credentials so neither the account's existence nor which field
/// was wrong is revealed (AC-CUS-001.5).
/// </summary>
public record CustomerLoginCommand(string Email, string Password, string? RecaptchaToken = null) : IRequest<AuthResponse>;

public class CustomerLoginCommandValidator : AbstractValidator<CustomerLoginCommand>
{
    public CustomerLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CustomerLoginCommandHandler : IRequestHandler<CustomerLoginCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _current;
    private readonly IRecaptchaVerifier _recaptcha;

    public CustomerLoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        IDateTimeProvider clock,
        ICurrentUserService current,
        IRecaptchaVerifier recaptcha)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _current = current;
        _recaptcha = recaptcha;
    }

    public async Task<AuthResponse> Handle(CustomerLoginCommand request, CancellationToken cancellationToken)
    {
        await _recaptcha.VerifyOrThrowAsync(RecaptchaFormType.Login, request.RecaptchaToken, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Customer)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

        // Generic message + password verification even on miss so we never reveal which check failed.
        if (user is null || !_hasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.EmailVerified)
            throw new UnauthorizedException("Please verify your email address before signing in.");

        user.LastLoginOnUtc = _clock.UtcNow;

        var (roles, modules) = AccessScope.From(user.UserRoles);
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user, roles, modules);
        var refreshToken = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashRefreshToken(refreshToken),
            CreatedOnUtc = _clock.UtcNow,
            ExpiresOnUtc = _clock.UtcNow.AddDays(_tokens.RefreshTokenDays),
            CreatedByIp = _current.IpAddress,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return AuthResponseFactory.Create(user, roles, modules, accessToken, expiresAt, refreshToken);
    }
}
