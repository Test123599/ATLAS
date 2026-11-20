using System.ComponentModel.DataAnnotations;

namespace MediaLib.Shared.Validation;

/// <summary>
/// Validates ISBN-10 and ISBN-13 including their check digit (not merely their length).
/// Separators (spaces and hyphens) are accepted and ignored.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IsbnAttribute : ValidationAttribute
{
    public IsbnAttribute() : base("'{0}' is not a valid ISBN-10 or ISBN-13 identifier.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // Required handles null/empty.

        var raw = value.ToString()?.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        if (string.IsNullOrEmpty(raw)) return false;

        return raw.Length switch
        {
            10 => IsValidIsbn10(raw),
            13 => IsValidIsbn13(raw),
            _ => false,
        };
    }

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i])) return false;
            sum += (10 - i) * (isbn[i] - '0');
        }

        var check = isbn[9];
        var checkValue = check == 'X' || check == 'x' ? 10 : check - '0';
        if (checkValue is < 0 or > 10) return false;

        return (sum + checkValue) % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            if (!char.IsDigit(isbn[i])) return false;
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }
}
