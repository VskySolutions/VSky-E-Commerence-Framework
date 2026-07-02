using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Categories;

public record GetCategoryQuery(Guid Id) : IRequest<CategoryDto>;

public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, CategoryDto>
{
    private readonly IApplicationDbContext _db;

    public GetCategoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CategoryDto> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        return CategoryDto.From(category);
    }
}
