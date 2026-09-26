using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot;

/// <summary>What the window shows: HTML text and its buttons.</summary>
public record BotScreen(string Text, InlineKeyboardMarkup Keyboard);
