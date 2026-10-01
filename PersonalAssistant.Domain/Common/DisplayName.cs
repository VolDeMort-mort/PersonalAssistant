namespace PersonalAssistant.Domain.Common;

/// <summary>
/// Names people give to their categories and topics: "їжа" typed by the user means "🍔 Їжа".
/// </summary>
internal static class DisplayName
{
    /// <summary>Same name for a human: emoji, spaces, punctuation and case are ignored.</summary>
    public static bool AreSame(string name, string other)
    {
        var mine = LettersAndDigits(name);
        var theirs = LettersAndDigits(other);

        // Names made of emoji only have no letters to compare
        return mine.Length == 0 || theirs.Length == 0
            ? string.Equals(name.Trim(), other.Trim(), StringComparison.Ordinal)
            : string.Equals(mine, theirs, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string LettersAndDigits(string text) => string.Concat(text.Where(char.IsLetterOrDigit));
}
