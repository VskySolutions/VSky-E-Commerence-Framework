using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.IntegrationCredentials;

/// <summary>Summary row shown in the admin list — never carries secret fields.</summary>
public class IntegrationCredentialListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static IntegrationCredentialListItemDto From(IntegrationCredentialBase e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Active = e.Active,
        IsProduction = e.IsProduction,
        CreatedOnUtc = e.CreatedOnUtc,
        UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

/// <summary>
/// Shared CRUD plumbing for the homogeneous per-integration credential tables, keeping each provider slice
/// down to its typed fields. Soft-delete and audit stamping happen in <c>AppDbContext</c>.
/// </summary>
internal static class IntegrationCredentialSupport
{
    /// <summary>Trims a field, collapsing blank input to null.</summary>
    public static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Active row(s) first, then alphabetical — the shape the admin grid renders.</summary>
    public static async Task<IReadOnlyList<IntegrationCredentialListItemDto>> ListAsync<T>(
        DbSet<T> set, CancellationToken ct) where T : IntegrationCredentialBase
    {
        var rows = await set.AsNoTracking()
            .OrderByDescending(x => x.Active)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(IntegrationCredentialListItemDto.From).ToList();
    }

    public static async Task<T> GetAsync<T>(DbSet<T> set, Guid id, CancellationToken ct)
        where T : IntegrationCredentialBase
        => await set.FirstOrDefaultAsync(x => x.Id == id, ct)
           ?? throw new NotFoundException(typeof(T).Name, id);

    public static async Task DeleteAsync<T>(
        DbSet<T> set, IApplicationDbContext db, Guid id, CancellationToken ct) where T : IntegrationCredentialBase
    {
        var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new NotFoundException(typeof(T).Name, id);
        set.Remove(entity); // converted to a soft-delete on save by AppDbContext
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Sets the Active flag on one row (the admin grid's quick toggle). Activating a row deactivates every
    /// other row of the same integration, keeping at most one active — the row the runtime resolver reads.
    /// </summary>
    public static async Task SetActiveAsync<T>(
        DbSet<T> set, IApplicationDbContext db, Guid id, bool active, CancellationToken ct) where T : IntegrationCredentialBase
    {
        var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new NotFoundException(typeof(T).Name, id);
        entity.Active = active;

        if (active)
        {
            var others = await set.Where(x => x.Active && x.Id != id).ToListAsync(ct);
            foreach (var other in others)
                other.Active = false;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Loads (by id) or creates the row and applies the common fields. When the row is being set Active,
    /// every other row of the same integration is deactivated so at most one stays active — that is the
    /// row the runtime resolver reads. Returns the tracked entity; the caller sets its typed fields and
    /// then saves.
    /// </summary>
    public static async Task<T> UpsertAsync<T>(
        DbSet<T> set, Guid? id, string name, bool active, bool isProduction, CancellationToken ct)
        where T : IntegrationCredentialBase, new()
    {
        T entity;
        if (id is { } existingId)
        {
            entity = await set.FirstOrDefaultAsync(x => x.Id == existingId, ct)
                     ?? throw new NotFoundException(typeof(T).Name, existingId);
        }
        else
        {
            entity = new T();
            set.Add(entity);
        }

        entity.Name = name.Trim();
        entity.Active = active;
        entity.IsProduction = isProduction;

        if (active)
        {
            var others = await set.Where(x => x.Active && x.Id != entity.Id).ToListAsync(ct);
            foreach (var other in others)
                other.Active = false;
        }

        return entity;
    }
}
