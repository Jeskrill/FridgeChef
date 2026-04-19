using System.Text.RegularExpressions;

namespace FridgeChef.Domain.Common;

/// <summary>
/// Validated email address value object.
/// </summary>
public readonly partial record struct EmailAddress
{
    private static readonly Regex EmailRegex = CreateEmailRegex();

    public string Value { get; }

    public EmailAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex CreateEmailRegex();
}

/// <summary>
/// URL-friendly slug for recipes.
/// </summary>
public readonly record struct Slug(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Monetary amount in rubles.
/// </summary>
public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);
    public override string ToString() => $"{Amount:F2} ₽";
}
