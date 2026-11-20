using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MediaLib.Shared.Validation;

/// <summary>Mirrors the API Identity password policy: 8+ chars, one uppercase, one digit.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed partial class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
        : base("Password must contain at least 8 characters, one uppercase letter and one digit.")
    {
    }

    public override bool IsValid(object? value)
        => value is string password && PasswordRegex().IsMatch(password);

    [GeneratedRegex(@"^(?=.*[A-Z])(?=.*\d).{8,}$", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordRegex();
}
