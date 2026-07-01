namespace VSky.Infrastructure.Security;

/// <summary>JWT configuration bound from the "Jwt" configuration section.</summary>
public class JwtOptions
{
    public string Issuer { get; set; } = "VSky.ECommerce";
    public string Audience { get; set; } = "VSky.ECommerce.Client";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public string PrivateKeyPath { get; set; } = "keys/jwt-private.pem";
}
