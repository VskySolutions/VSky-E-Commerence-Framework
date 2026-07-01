namespace VSky.API.Authorization;

/// <summary>
/// Authorization policy names for the three-tier role model. Each policy admits its own role and all
/// higher roles (SuperAdmin ⊇ TenantAdmin ⊇ Operator).
/// </summary>
public static class Policies
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Operator = "Operator";
}
