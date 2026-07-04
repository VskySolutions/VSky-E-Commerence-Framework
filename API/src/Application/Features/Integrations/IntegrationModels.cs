using VSky.Domain.Entities;

namespace VSky.Application.Features.Integrations;

/// <summary>Renders a secret value as a masked hint — only the last four chars are ever exposed (AC-TEN-002.6).</summary>
public static class CredentialMasking
{
    public static string Mask(string? lastFour) =>
        string.IsNullOrEmpty(lastFour) ? "••••" : $"•••• {lastFour}";
}

/// <summary>A provider grouped under its category, with configuration status but no field values.</summary>
public class IntegrationProviderSummaryDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public int FieldCount { get; set; }
    public int RequiredFieldCount { get; set; }
    public int ConfiguredFieldCount { get; set; }
    public bool IsConfigured { get; set; }

    /// <summary>Requires <paramref name="p"/>.Definitions and .Credentials to be loaded.</summary>
    public static IntegrationProviderSummaryDto From(IntegrationProvider p)
    {
        var configuredDefIds = p.Credentials.Select(c => c.DefinitionId).ToHashSet();
        var requiredDefs = p.Definitions.Where(d => d.IsRequired).ToList();

        return new IntegrationProviderSummaryDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            CategoryCode = p.Category?.Code ?? string.Empty,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            IsEnabled = p.IsEnabled,
            DisplayOrder = p.DisplayOrder,
            FieldCount = p.Definitions.Count,
            RequiredFieldCount = requiredDefs.Count,
            ConfiguredFieldCount = p.Credentials.Count,
            IsConfigured = p.Definitions.Count > 0
                && p.Credentials.Count > 0
                && requiredDefs.All(d => configuredDefIds.Contains(d.Id)),
        };
    }
}

/// <summary>A category with its providers (grouped view for the admin credential screen, AC-TEN-002.1).</summary>
public class IntegrationCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<IntegrationProviderSummaryDto> Providers { get; set; } = new();

    public static IntegrationCategoryDto From(IntegrationCategory c, IEnumerable<IntegrationProviderSummaryDto> providers) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Code = c.Code,
        Description = c.Description,
        DisplayOrder = c.DisplayOrder,
        Providers = providers.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList(),
    };
}

/// <summary>A single credential field definition (the metadata that drives the auto-generated form).</summary>
public class CredentialDefinitionDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldCode { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int DisplayOrder { get; set; }

    public static CredentialDefinitionDto From(CredentialDefinition d) => new()
    {
        Id = d.Id,
        ProviderId = d.ProviderId,
        FieldName = d.FieldName,
        FieldCode = d.FieldCode,
        DataType = d.DataType.ToString(),
        IsRequired = d.IsRequired,
        IsSecret = d.IsSecret,
        Placeholder = d.Placeholder,
        HelpText = d.HelpText,
        DisplayOrder = d.DisplayOrder,
    };
}

/// <summary>A form field = its definition plus the current (masked) stored value.</summary>
public class ProviderFormFieldDto
{
    public Guid DefinitionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldCode { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasValue { get; set; }

    /// <summary>Masked hint for a stored secret; null when unset.</summary>
    public string? MaskedValue { get; set; }

    /// <summary>Plaintext for non-secret fields only; secret values are never returned (AC-TEN-002.6/7).</summary>
    public string? Value { get; set; }

    public static ProviderFormFieldDto From(CredentialDefinition d, IntegrationCredential? value) => new()
    {
        DefinitionId = d.Id,
        FieldName = d.FieldName,
        FieldCode = d.FieldCode,
        DataType = d.DataType.ToString(),
        IsRequired = d.IsRequired,
        IsSecret = d.IsSecret,
        Placeholder = d.Placeholder,
        HelpText = d.HelpText,
        DisplayOrder = d.DisplayOrder,
        HasValue = value is not null,
        MaskedValue = value is null ? null : d.IsSecret ? CredentialMasking.Mask(value.LastFourChars) : null,
        Value = value is not null && !d.IsSecret ? value.Value : null,
    };
}

/// <summary>The complete auto-generated credential form for a provider: metadata + current masked values.</summary>
public class ProviderFormDto
{
    public IntegrationProviderSummaryDto Provider { get; set; } = new();
    public List<ProviderFormFieldDto> Fields { get; set; } = new();
}
