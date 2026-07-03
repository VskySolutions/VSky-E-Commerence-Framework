using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Rma;

// ---- Admin: list + get -------------------------------------------------------

/// <summary>Lists returns for admin review, newest first, optionally filtered by status (AC-ORD-004.3).</summary>
public record ListRmasQuery(string? Status = null, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<RmaDto>>;

public class ListRmasQueryHandler : IRequestHandler<ListRmasQuery, PaginatedList<RmaDto>>
{
    private readonly IApplicationDbContext _db;
    public ListRmasQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<RmaDto>> Handle(ListRmasQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Rma> query = _db.Rmas.AsNoTracking().Include(r => r.Lines);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<RmaStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(r => r.Status == status);

        var page = await PaginatedList<Domain.Entities.Rma>.CreateAsync(
            query.OrderByDescending(r => r.RequestedOnUtc), request.Page, request.PageSize, cancellationToken);
        return new PaginatedList<RmaDto>(page.Items.Select(RmaDto.From).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
    }
}

/// <summary>Gets one return (admin).</summary>
public record GetRmaQuery(Guid Id) : IRequest<RmaDto>;

public class GetRmaQueryHandler : IRequestHandler<GetRmaQuery, RmaDto>
{
    private readonly IApplicationDbContext _db;
    public GetRmaQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<RmaDto> Handle(GetRmaQuery request, CancellationToken cancellationToken)
    {
        var rma = await _db.Rmas.AsNoTracking().Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Rma), request.Id);
        return RmaDto.From(rma);
    }
}

// ---- Buyer: my returns -------------------------------------------------------

/// <summary>Lists the current customer's own returns, newest first (AC-ORD-004.1).</summary>
public record ListMyRmasQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedList<RmaDto>>;

public class ListMyRmasQueryHandler : IRequestHandler<ListMyRmasQuery, PaginatedList<RmaDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public ListMyRmasQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<PaginatedList<RmaDto>> Handle(ListMyRmasQuery request, CancellationToken cancellationToken)
    {
        if (_current.UserId is not Guid userId)
            throw new UnauthorizedException("Authentication is required.");

        var customerId = await _db.Customers.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (customerId == Guid.Empty)
            throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var query = _db.Rmas.AsNoTracking().Include(r => r.Lines)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.RequestedOnUtc);

        var page = await PaginatedList<Domain.Entities.Rma>.CreateAsync(query, request.Page, request.PageSize, cancellationToken);
        return new PaginatedList<RmaDto>(page.Items.Select(RmaDto.From).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
    }
}
