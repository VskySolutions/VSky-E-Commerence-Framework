using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.ShippingMethods;

/// <summary>Creates a custom shipping method with optional per-zone rate overrides (REQ-SHP-003).</summary>
public record CreateShippingMethodCommand(
    string Name,
    ShippingMethodType MethodType,
    decimal? FlatRate = null,
    decimal? FreeShippingThreshold = null,
    string? TiersJson = null,
    bool IsEnabled = true,
    int DisplayOrder = 0,
    List<ShippingMethodZoneRateInput>? ZoneRates = null) : IRequest<ShippingMethodDto>;

public class CreateShippingMethodCommandValidator : AbstractValidator<CreateShippingMethodCommand>
{
    public CreateShippingMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MethodType).IsInEnum();
        RuleFor(x => x.FlatRate).GreaterThanOrEqualTo(0).When(x => x.FlatRate.HasValue);
        RuleFor(x => x.FreeShippingThreshold).GreaterThanOrEqualTo(0).When(x => x.FreeShippingThreshold.HasValue);
        RuleForEach(x => x.ZoneRates).ChildRules(r =>
        {
            r.RuleFor(z => z.ShippingZoneId).NotEmpty();
            r.RuleFor(z => z.Rate).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateShippingMethodCommandHandler : IRequestHandler<CreateShippingMethodCommand, ShippingMethodDto>
{
    private readonly IApplicationDbContext _db;

    public CreateShippingMethodCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ShippingMethodDto> Handle(CreateShippingMethodCommand request, CancellationToken cancellationToken)
    {
        var zoneRateInputs = request.ZoneRates ?? new List<ShippingMethodZoneRateInput>();

        // Collapse to one rate per zone (last wins) so the unique (method, zone) index is never violated.
        var desired = zoneRateInputs
            .GroupBy(i => i.ShippingZoneId)
            .ToDictionary(g => g.Key, g => g.Last().Rate);

        // Every referenced zone must exist; keep the resolved entities so the response can echo zone names.
        var zoneIds = desired.Keys.ToList();
        var zones = zoneIds.Count == 0
            ? new List<ShippingZone>()
            : await _db.ShippingZones.Where(z => zoneIds.Contains(z.Id)).ToListAsync(cancellationToken);
        var missing = zoneIds.Except(zones.Select(z => z.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException(nameof(ShippingZone), missing[0]);
        var zonesById = zones.ToDictionary(z => z.Id);

        var entity = new ShippingMethod
        {
            Name = request.Name,
            MethodType = request.MethodType,
            FlatRate = request.FlatRate,
            FreeShippingThreshold = request.FreeShippingThreshold,
            TiersJson = request.TiersJson,
            IsEnabled = request.IsEnabled,
            DisplayOrder = request.DisplayOrder,
        };

        foreach (var kvp in desired)
            entity.ZoneRates.Add(new ShippingMethodZoneRate { ShippingZone = zonesById[kvp.Key], Rate = kvp.Value });

        _db.ShippingMethods.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ShippingMethodDto.From(entity);
    }
}
