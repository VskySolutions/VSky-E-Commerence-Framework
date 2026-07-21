using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Utilities;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CmsPageGroups;

/// <summary>Creates a new CMS page group.</summary>
public record CreateCmsPageGroupCommand(
    string Name,
    string? Slug = null,
    int DisplayOrder = 0) : IRequest<CmsPageGroupDto>;

public class CreateCmsPageGroupCommandValidator : AbstractValidator<CreateCmsPageGroupCommand>
{
    public CreateCmsPageGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(220);
    }
}

public class CreateCmsPageGroupCommandHandler : IRequestHandler<CreateCmsPageGroupCommand, CmsPageGroupDto>
{
    private readonly IApplicationDbContext _db;

    public CreateCmsPageGroupCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageGroupDto> Handle(CreateCmsPageGroupCommand request, CancellationToken cancellationToken)
    {
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);

        if (await _db.CMSPageGroups.AnyAsync(g => g.Slug == slug, cancellationToken))
            throw new ConflictException($"A CMS page group with slug '{slug}' already exists.");

        var entity = new CMSPageGroup
        {
            Name = request.Name,
            Slug = slug,
            DisplayOrder = request.DisplayOrder,
        };

        _db.CMSPageGroups.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return CmsPageGroupDto.From(entity);
    }
}
