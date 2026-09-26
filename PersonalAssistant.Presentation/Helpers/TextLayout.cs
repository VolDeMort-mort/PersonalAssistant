using System.Text;

namespace PersonalAssistant.Presentation.Helpers;

/// <summary>
/// Telegram has no text alignment, so a title is centered by hand with leading non-breaking spaces
/// (regular leading spaces are trimmed). Widths are estimates for Telegram's sans-serif fonts in em,
/// so centering is approximate and may differ by a space between Desktop, Android and iOS.
/// </summary>
public static class TextLayout
{
    private const char NonBreakingSpace = ' ';
    private const double SpaceWidth = 0.27;
    private const double BoldFactor = 1.06;

    /// <summary>Spaces that center <paramref name="text"/> above <paramref name="line"/>.</summary>
    public static string CenterPadding(string text, string line, bool bold)
    {
        var free = Width(line) - Width(text) * (bold ? BoldFactor : 1);
        var spaces = (int)Math.Round(free / 2 / SpaceWidth);
        return new string(NonBreakingSpace, Math.Max(0, spaces));
    }

    public static double Width(string text) => text.EnumerateRunes().Sum(Width);

    private static double Width(Rune rune)
    {
        var code = rune.Value;

        if (code == '─') return 0.6;                              // box drawing, comes from a fallback font
        if (code is 0xFE0F or 0x200D) return 0;                   // emoji variation selector and joiner
        if (code >= 0x1F000 || code is >= 0x2190 and <= 0x2BFF) return 1.25; // emoji are drawn as images
        if (Rune.IsWhiteSpace(rune) || IsOneOf(rune, "іїІЇ'.,:;·!|()lijI")) return 0.27;
        if (Rune.IsDigit(rune)) return 0.57;
        if (IsOneOf(rune, "ШЩЖЮМФшщжюмфWMmw")) return 0.85;
        if (Rune.IsUpper(rune)) return 0.68;
        return 0.55;
    }

    private static bool IsOneOf(Rune rune, string chars) => rune.IsBmp && chars.Contains((char)rune.Value);
}
