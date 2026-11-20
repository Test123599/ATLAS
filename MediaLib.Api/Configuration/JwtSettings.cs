namespace MediaLib.Api.Configuration;

/// <summary>Bound from the Jwt section of appsettings*.json.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ATLAS-MediaLib";

    public string Audience { get; set; } = "ATLAS-MediaLib-Client";

    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key) || Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must contain at least 32 characters. Override it with a strong secret outside local development.");
        }

        if (AccessTokenMinutes <= 0 || RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("Jwt token lifetimes must be positive.");
        }
    }
}
