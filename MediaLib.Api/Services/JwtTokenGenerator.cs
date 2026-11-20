using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediaLib.Api.Configuration;
using MediaLib.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace MediaLib.Api.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(Utilisateur utilisateur, IReadOnlyList<string> roles);
}

/// <summary>Creates short-lived signed access tokens with identity and role claims.</summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings) => _settings = settings;

    public (string Token, DateTime ExpiresAt) Generate(Utilisateur utilisateur, IReadOnlyList<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, utilisateur.Id),
            new(JwtRegisteredClaimNames.Email, utilisateur.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, utilisateur.Id),
            new(ClaimTypes.Email, utilisateur.Email ?? string.Empty),
            new(ClaimTypes.GivenName, utilisateur.Prenom),
            new(ClaimTypes.Surname, utilisateur.Nom),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
