using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.ProductQuestions;

/// <summary>Admin: approve or reject a product question, setting its moderation status (WO-58).</summary>
public record ModerateProductQuestionCommand(Guid Id, QuestionStatus Status) : IRequest<ProductQuestionDto>;

public class ModerateProductQuestionCommandValidator : AbstractValidator<ModerateProductQuestionCommand>
{
    public ModerateProductQuestionCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class ModerateProductQuestionCommandHandler : IRequestHandler<ModerateProductQuestionCommand, ProductQuestionDto>
{
    private readonly IApplicationDbContext _db;

    public ModerateProductQuestionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductQuestionDto> Handle(ModerateProductQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductQuestions
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductQuestion), request.Id);

        entity.Status = request.Status;

        await _db.SaveChangesAsync(cancellationToken);
        return ProductQuestionDto.From(entity);
    }
}
