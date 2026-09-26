using PersonalAssistant.Presentation.Bot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Services;

public class BotMessenger : IBotMessenger
{
    private readonly ITelegramBotClient _botClient;
    private readonly IScreenTracker _screens;
    private readonly ILogger<BotMessenger> _logger;

    public BotMessenger(ITelegramBotClient botClient, IScreenTracker screens, ILogger<BotMessenger> logger)
    {
        _botClient = botClient;
        _screens = screens;
        _logger = logger;
    }

    public async Task ShowScreenAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var screenId = _screens.Get(chatId);
        if (screenId is not null && await TryEditAsync(chatId, screenId.Value, text, replyMarkup, cancellationToken))
            return;

        await OpenScreenAsync(chatId, text, replyMarkup, cancellationToken);
    }

    public async Task OpenScreenAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var oldScreenId = _screens.Get(chatId);

        // Send first, delete after: if sending fails the old window is still there
        var newScreenId = await SendAsync(chatId, text, replyMarkup, cancellationToken);
        _screens.Set(chatId, newScreenId);

        if (oldScreenId is not null)
            await DeleteAsync(chatId, oldScreenId.Value, cancellationToken);
    }

    public async Task<int> SendAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var message = await _botClient.SendMessage(chatId, text,
            parseMode: ParseMode.Html, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        return message.MessageId;
    }

    public async Task DeleteAsync(long chatId, int messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.DeleteMessage(chatId, messageId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cosmetic operation: a message that is already gone must not break the flow
            _logger.LogWarning(ex, "Could not delete message {MessageId} in chat {ChatId}", messageId, chatId);
        }
    }

    public async Task AnswerCallbackAsync(string callbackQueryId, string? text = null, bool showAlert = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.AnswerCallbackQuery(callbackQueryId, text, showAlert, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to answer callback query {CallbackQueryId}", callbackQueryId);
        }
    }

    private async Task<bool> TryEditAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup, CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.EditMessageText(chatId, messageId, text,
                parseMode: ParseMode.Html, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            return true;
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
        {
            // The window already shows exactly this screen
            return true;
        }
        catch (ApiRequestException ex)
        {
            // Deleted by the user, too old, etc.: the caller will open a new window
            _logger.LogInformation("Could not edit window {MessageId} in chat {ChatId}: {Reason}", messageId, chatId, ex.Message);
            return false;
        }
    }
}
