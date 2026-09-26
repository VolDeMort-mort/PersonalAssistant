using System.Globalization;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Helpers;

/// <summary>
/// Whole hryvnias: "12 450 ₴", "+25 000 ₴", "−30 ₴", and parsing of what the user types.
/// </summary>
public static class MoneyFormat
{
    /// <summary>Stops typos like extra zeros; also keeps sums far from overflow.</summary>
    public const long MaxAmount = 1_000_000_000;

    // Non-breaking spaces keep "12 450 ₴" on one line and aligned in monospace blocks
    private const string Space = " ";

    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberGroupSeparator = Space,
        NegativeSign = "−"
    };

    public static string Amount(long value) => $"{value.ToString("#,0", Numbers)}{Space}₴";

    /// <summary>Always shows the direction: "+25 000 ₴" or "−30 ₴".</summary>
    public static string Signed(long value) => value > 0 ? $"+{Amount(value)}" : Amount(value);

    public static string Signed(long amount, TransactionType type) =>
        Signed(type == TransactionType.Income ? amount : -amount);

    /// <summary>Accepts "250", "1 500", "1500 грн", "-500"; rejects kopecks like "30.50".</summary>
    public static bool TryParse(string? text, bool allowZeroOrNegative, out long amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text
            .Replace("₴", "")
            .Replace("грн", "", StringComparison.OrdinalIgnoreCase)
            .Replace('−', '-');
        cleaned = string.Concat(cleaned.Where(c => !char.IsWhiteSpace(c)));

        if (!long.TryParse(cleaned, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount))
            return false;

        return amount is >= -MaxAmount and <= MaxAmount && (allowZeroOrNegative || amount > 0);
    }
}
