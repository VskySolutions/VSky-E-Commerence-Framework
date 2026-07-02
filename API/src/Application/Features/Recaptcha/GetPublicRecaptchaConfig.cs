using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Recaptcha;

/// <summary>Public reCAPTCHA config for the storefront widget — never includes the Secret Key (AC-TEN-007.5).</summary>
public record GetPublicRecaptchaConfigQuery : IRequest<PublicRecaptchaConfigDto>;

public class GetPublicRecaptchaConfigQueryHandler : IRequestHandler<GetPublicRecaptchaConfigQuery, PublicRecaptchaConfigDto>
{
    private readonly IApplicationDbContext _db;

    public GetPublicRecaptchaConfigQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicRecaptchaConfigDto> Handle(GetPublicRecaptchaConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _db.RecaptchaConfigs.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                     ?? new RecaptchaConfig();
        return PublicRecaptchaConfigDto.From(config);
    }
}
