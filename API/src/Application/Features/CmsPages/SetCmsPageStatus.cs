using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CmsPages;

/// <summary>Transitions a CMS page's publish lifecycle (Draft / Published / Archived).</summary>
public record SetCmsPageStatusCommand(Guid Id, CmsContentStatus Status) : IRequest<CmsPageDto>;

public class SetCmsPageStatusCommandValidator : AbstractValidator<SetCmsPageStatusCommand>
{
    public SetCmsPageStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class SetCmsPageStatusCommandHandler : IRequestHandler<SetCmsPageStatusCommand, CmsPageDto>
{
    private readonly IApplicationDbContext _db;

    public SetCmsPageStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageDto> Handle(SetCmsPageStatusCommand request, CancellationToken cancellationToken)
    {
        var page = await _db.CMSPages
            .Include(p => p.PageGroup)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSPage), request.Id);

        page.Status = request.Status;

        await _db.SaveChangesAsync(cancellationToken);
        return CmsPageDto.From(page);
    }
}
