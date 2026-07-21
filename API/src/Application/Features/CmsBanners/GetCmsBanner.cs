using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CmsBanners;

public record GetCmsBannerQuery(Guid Id) : IRequest<CmsBannerDto>;

public class GetCmsBannerQueryHandler : IRequestHandler<GetCmsBannerQuery, CmsBannerDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsBannerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsBannerDto> Handle(GetCmsBannerQuery request, CancellationToken cancellationToken)
    {
        var banner = await _db.CMSBanners
            .AsNoTracking()
            .Include(b => b.ImageMedia)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSBanner), request.Id);

        return CmsBannerDto.From(banner);
    }
}
