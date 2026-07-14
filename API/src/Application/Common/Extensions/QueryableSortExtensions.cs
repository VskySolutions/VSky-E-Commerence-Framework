using System.Linq.Expressions;

namespace VSky.Application.Common.Extensions;

/// <summary>
/// Server-side sorting helpers for list endpoints. <see cref="ApplySort"/> keeps every admin list
/// consistent: a caller-supplied <c>sortBy</c> is honoured only when it maps to an allow-listed property
/// (guarding against arbitrary/invalid columns), otherwise the list falls back to a stable default
/// (newest-first on <c>CreatedOnUtc</c>). Ordering is built as a normal, typed <c>OrderBy</c> expression
/// so EF Core translates it to SQL exactly as a hand-written one would.
/// </summary>
public static class QueryableSortExtensions
{
    /// <summary>
    /// Orders by the mapped property for <paramref name="sortBy"/> (from <paramref name="allowed"/>), or by
    /// <paramref name="defaultProperty"/> descending when it is absent/unrecognised. Property names may be
    /// dotted paths across navigations (e.g. "User.Email").
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        bool descending,
        IReadOnlyDictionary<string, string> allowed,
        string defaultProperty = "CreatedOnUtc",
        bool defaultDescending = true)
    {
        var property = defaultProperty;
        var desc = defaultDescending;

        if (!string.IsNullOrWhiteSpace(sortBy) && allowed != null && allowed.TryGetValue(sortBy.Trim(), out var mapped))
        {
            property = mapped;
            desc = descending;
        }

        return query.OrderByProperty(property, desc);
    }

    /// <summary>
    /// Builds a typed <c>OrderBy</c>/<c>OrderByDescending</c> for the given (optionally dotted) property path.
    /// The key type is the property's real type, so value-type columns sort without boxing surprises.
    /// </summary>
    public static IQueryable<T> OrderByProperty<T>(this IQueryable<T> query, string propertyPath, bool descending)
    {
        var param = Expression.Parameter(typeof(T), "x");
        Expression body = param;
        foreach (var member in propertyPath.Split('.'))
            body = Expression.PropertyOrField(body, member);

        var keySelector = Expression.Lambda(body, param);
        var method = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var call = Expression.Call(
            typeof(Queryable),
            method,
            new[] { typeof(T), body.Type },
            query.Expression,
            Expression.Quote(keySelector));

        return query.Provider.CreateQuery<T>(call);
    }
}
