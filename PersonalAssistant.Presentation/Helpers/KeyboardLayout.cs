using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Helpers;

/// <summary>
/// Buttons in one row share its width equally, so a long text in a half-width button gets cut.
/// Short buttons go two per row, long ones take a whole row.
/// </summary>
public static class KeyboardLayout
{
    // Roughly what fits into half a row on a narrow phone, in em (see TextLayout)
    private const double HalfRowWidth = 9.5;

    public static IEnumerable<InlineKeyboardButton[]> Pack(IEnumerable<InlineKeyboardButton> buttons)
    {
        InlineKeyboardButton? waiting = null; // a short button looking for a pair

        foreach (var button in buttons)
        {
            if (!IsShort(button))
            {
                yield return new[] { button };
                continue;
            }

            if (waiting is null)
            {
                waiting = button;
                continue;
            }

            yield return new[] { waiting, button };
            waiting = null;
        }

        if (waiting is not null)
            yield return new[] { waiting };
    }

    /// <summary>Equal-width grid for buttons of the same kind: days, hours, months.</summary>
    public static IEnumerable<InlineKeyboardButton[]> Grid(IEnumerable<InlineKeyboardButton> buttons, int perRow) =>
        buttons.Chunk(perRow);

    private static bool IsShort(InlineKeyboardButton button) => TextLayout.Width(button.Text) <= HalfRowWidth;
}
