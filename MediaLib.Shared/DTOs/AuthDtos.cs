using System.ComponentModel.DataAnnotations;
using MediaLib.Shared.Validation;

namespace MediaLib.Shared.DTOs;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Prenom { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Nom { get; set; } = string.Empty;

    [Required]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string MotDePasseActuel { get; set; } = string.Empty;

    [Required]
    [StrongPassword]
    public string NouveauMotDePasse { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(120)]
    public string Prenom { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Nom { get; set; } = string.Empty;
}

/// <summary>Authenticated user payload. Never contains a password hash or security stamp.</summary>
public sealed record UserProfileDto(
    string Id,
    string Email,
    string Prenom,
    string Nom,
    DateTime DateInscription,
    IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto Utilisateur);
