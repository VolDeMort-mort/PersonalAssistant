using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Services;

/// <summary>
/// All texts are sent with HTML markup: user-provided text must be HTML-encoded before it gets here.
/// </summary>
public interface IBotMessenger
{
    /// <summary>Draws a screen into the chat's window; sends a new window if there is none or it can't be edited.</summary>
    Task ShowScreenAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);

    /// <summary>Replaces the chat's window with a fresh message at the bottom of the chat.</summary>
    Task OpenScreenAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);

    /// <summary>Sends a standalone message that is not a window. Returns its id.</summary>
    Task<int> SendAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(long chatId, int messageId, CancellationToken cancellationToken = default);

    /// <summary>Stops the button's loading spinner; with text shows a toast (or a dialog when showAlert is true).</summary>
    Task AnswerCallbackAsync(string callbackQueryId, string? text = null, bool showAlert = false, CancellationToken cancellationToken = default);
}
