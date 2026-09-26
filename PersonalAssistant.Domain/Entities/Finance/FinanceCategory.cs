using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>
/// What money was spent on or where it came from, e.g. "🍔 Їжа" or "💼 Зарплата".
/// A category belongs to one transaction type, so it also decides whether money goes in or out.
/// </summary>
public class FinanceCategory
{
    public const int MaxNameLength = 40;

    private FinanceCategory() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public TransactionType Type { get; private set; }
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }

    public static FinanceCategory Create(long chatId, TransactionType type, string name, int sortOrder)
    {
        return new FinanceCategory
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Type = type,
            Name = Guard.RequiredText(name, MaxNameLength, nameof(name)),
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Same category for a human: "їжа" matches "🍔 Їжа". Emoji, spaces, punctuation and case are ignored.
    /// </summary>
    public bool HasSameName(string name)
    {
        var mine = LettersAndDigits(Name);
        var other = LettersAndDigits(name);

        // Names made of emoji only have no letters to compare
        return mine.Length == 0 || other.Length == 0
            ? string.Equals(Name, name.Trim(), StringComparison.Ordinal)
            : string.Equals(mine, other, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string LettersAndDigits(string text) => string.Concat(text.Where(char.IsLetterOrDigit));
}
