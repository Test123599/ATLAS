using System.Security.Cryptography;
using MediaLib.Api.Common;
using MediaLib.Api.Configuration;
using MediaLib.Api.Data;
using MediaLib.Api.Domain;
using MediaLib.Api.Mapping;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface IAuthService
{
    Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(string utilisateurId, string refreshToken, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(string utilisateurId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(string utilisateurId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(string utilisateurId, ChangePasswordRequest request, CancellationToken ct = default);
    Task RevokeAllRefreshTokensAsync(string utilisateurId, CancellationToken ct = default);
}

/// <summary>Identity, signed access tokens and hashed, rotating refresh tokens.</summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<Utilisateur> _users;
    private readonly SignInManager<Utilisateur> _signIn;
    private readonly MediaLibDbContext _db;
    private readonly IJwtTokenGenerator _tokens;
    private readonly JwtSettings _settings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<Utilisateur> users,
        SignInManager<Utilisateur> signIn,
        MediaLibDbContext db,
        IJwtTokenGenerator tokens,
        JwtSettings settings,
        ILogger<AuthService> logger)
    {
        _users = users;
        _signIn = signIn;
        _db = db;
        _tokens = tokens;
        _settings = settings;
        _logger = logger;
    }

    public async Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw AppException.Conflict("Un compte utilise déjà cette adresse e-mail.", "EMAIL_DUPLIQUE");
        }

        var utilisateur = new Utilisateur
        {
            UserName = email,
            Email = email,
            Prenom = request.Prenom.Trim(),
            Nom = request.Nom.Trim(),
            DateInscription = DateTime.UtcNow,
            EstActif = true,
        };

        var result = await _users.CreateAsync(utilisateur, request.Password);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest(
                "Le compte n'a pas pu être créé.",
                "INSCRIPTION_INVALIDE",
                ToErrorDictionary(result));
        }

        var roleResult = await _users.AddToRoleAsync(utilisateur, Roles.Membre);
        if (!roleResult.Succeeded)
        {
            throw AppException.BadRequest("Le rôle du compte n'a pas pu être attribué.", "ROLE_INVALIDE", ToErrorDictionary(roleResult));
        }

        _logger.LogInformation("New member registered: {Email}", email);
        return utilisateur.ToProfileDto([Roles.Membre]);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var utilisateur = await _users.FindByEmailAsync(email);

        // Deliberately identical message for unknown email and wrong password.
        if (utilisateur is null)
        {
            throw AppException.Unauthorized("Identifiants invalides.", "IDENTIFIANTS_INVALIDES");
        }

        if (!utilisateur.EstActif)
        {
            throw AppException.Forbidden("Ce compte a été désactivé. Contactez un administrateur.", "COMPTE_DESACTIVE");
        }

        var result = await _signIn.CheckPasswordSignInAsync(utilisateur, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            throw new AppException(StatusCodes.Status423Locked, "COMPTE_VERROUILLE",
                "Compte verrouillé après 5 échecs. Réessayez dans quelques minutes.");
        }

        if (!result.Succeeded)
        {
            throw AppException.Unauthorized("Identifiants invalides.", "IDENTIFIANTS_INVALIDES");
        }

        await _users.ResetAccessFailedCountAsync(utilisateur);
        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();

        return await IssueTokensAsync(utilisateur, roles, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = HashToken(request.RefreshToken);
        var maintenant = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var stored = await _db.RefreshTokens
            .Include(t => t.Utilisateur)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw AppException.Unauthorized("Jeton de rafraîchissement invalide.", "REFRESH_INVALIDE");

        if (stored.RevoqueLe is not null)
        {
            // A stale token whose replacement is still live is a benign rotation race
            // (another tab/circuit refreshed first). Reject it without revoking the
            // live session. If there is no live replacement, treat it as theft.
            var remplacementActif = !string.IsNullOrWhiteSpace(stored.RemplaceParHash) &&
                await _db.RefreshTokens.AnyAsync(t =>
                    t.TokenHash == stored.RemplaceParHash &&
                    t.RevoqueLe == null &&
                    t.ExpireLe > maintenant, ct);

            if (!remplacementActif)
            {
                await RevokeAllRefreshTokensAsync(stored.UtilisateurId, ct);
                await transaction.CommitAsync(ct);
                _logger.LogWarning("Refresh token reuse detected for user {UserId}", stored.UtilisateurId);
                throw AppException.Unauthorized("Jeton de rafraîchissement réutilisé. Veuillez vous reconnecter.", "REFRESH_REUTILISE");
            }

            _logger.LogInformation("Stale refresh token presented for user {UserId}; live replacement kept.", stored.UtilisateurId);
            throw AppException.Unauthorized("Ce jeton a déjà été remplacé. Veuillez utiliser le jeton courant.", "REFRESH_STALE");
        }

        if (stored.ExpireLe <= maintenant)
        {
            throw AppException.Unauthorized("Jeton de rafraîchissement expiré.", "REFRESH_EXPIRE");
        }

        if (!stored.Utilisateur.EstActif)
        {
            throw AppException.Forbidden("Ce compte a été désactivé.", "COMPTE_DESACTIVE");
        }

        var roles = (await _users.GetRolesAsync(stored.Utilisateur)).ToList();
        var response = await IssueTokensAsync(stored.Utilisateur, roles, ct, stored);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task LogoutAsync(string utilisateurId, string refreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return;

        // [Authorize] alone is not enough: the supplied token must belong to the caller.
        if (stored.UtilisateurId != utilisateurId)
        {
            throw AppException.Unauthorized("Jeton de rafraîchissement invalide.", "REFRESH_INVALIDE");
        }

        if (stored.RevoqueLe is null)
        {
            stored.RevoqueLe = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserProfileDto> GetProfileAsync(string utilisateurId, CancellationToken ct = default)
    {
        var utilisateur = await _users.FindByIdAsync(utilisateurId)
                          ?? throw AppException.NotFound("Profil introuvable.");
        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();
        return utilisateur.ToProfileDto(roles);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string utilisateurId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var utilisateur = await _users.FindByIdAsync(utilisateurId)
                          ?? throw AppException.NotFound("Profil introuvable.");

        utilisateur.Prenom = request.Prenom.Trim();
        utilisateur.Nom = request.Nom.Trim();
        var result = await _users.UpdateAsync(utilisateur);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest("La mise à jour du profil a échoué.", "PROFIL_INVALIDE", ToErrorDictionary(result));
        }

        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();
        return utilisateur.ToProfileDto(roles);
    }

    public async Task ChangePasswordAsync(string utilisateurId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var utilisateur = await _users.FindByIdAsync(utilisateurId)
                          ?? throw AppException.NotFound("Profil introuvable.");

        var result = await _users.ChangePasswordAsync(utilisateur, request.MotDePasseActuel, request.NouveauMotDePasse);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest(
                "Le mot de passe n'a pas été modifié. Vérifiez l'ancien mot de passe et la politique de sécurité.",
                "MOT_DE_PASSE_INVALIDE",
                ToErrorDictionary(result));
        }

        await RevokeAllRefreshTokensAsync(utilisateurId, ct);
    }

    public async Task RevokeAllRefreshTokensAsync(string utilisateurId, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UtilisateurId == utilisateurId && t.RevoqueLe == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevoqueLe = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        Utilisateur utilisateur,
        IReadOnlyList<string> roles,
        CancellationToken ct,
        RefreshToken? previous = null)
    {
        var (accessToken, expiresAt) = _tokens.Generate(utilisateur, roles);

        var refreshValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var newHash = HashToken(refreshValue);

        var entity = new RefreshToken
        {
            UtilisateurId = utilisateur.Id,
            TokenHash = newHash,
            CreeLe = DateTime.UtcNow,
            ExpireLe = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays),
        };

        if (previous is not null)
        {
            previous.RevoqueLe = DateTime.UtcNow;
            previous.RemplaceParHash = newHash;
        }

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, refreshValue, expiresAt, utilisateur.ToProfileDto(roles));
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static Dictionary<string, string[]> ToErrorDictionary(IdentityResult result)
        => result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
