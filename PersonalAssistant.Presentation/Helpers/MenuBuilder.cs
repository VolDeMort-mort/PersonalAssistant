using PersonalAssistant.Presentation.Bot.Finance;
using PersonalAssistant.Presentation.Constants;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Helpers;

public static class MenuBuilder
{

    public static InlineKeyboardMarkup GetRootMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📓 Журнал", BotConstants.Payloads.NavJournal) },
            new[] { InlineKeyboardButton.WithCallbackData("💰 Фінанси", FinancePayloads.Home) },
            new[] { InlineKeyboardButton.WithCallbackData("📅 Планувальник", BotConstants.Payloads.NavPlanner) },
            new[] { InlineKeyboardButton.WithCallbackData("🌐 Інфа з сайтів", BotConstants.Payloads.NavScraper) }
        });
    }

    // Journal menu
    public static InlineKeyboardMarkup GetJournalMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🟢 Почати запис", BotConstants.Payloads.NavJournalStart),
                InlineKeyboardButton.WithCallbackData("🔙 Назад", BotConstants.Payloads.NavRoot)
            }
        });
    }

    // Journal in recording state
    public static InlineKeyboardMarkup GetJournalRecording()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🛑 Зберегти", BotConstants.Payloads.NavJournalRecorded),
                InlineKeyboardButton.WithCallbackData("Відмінити", BotConstants.Payloads.NavJournalCancelRecord)
            }
        });
    }

    // Journal in recorded state
    public static InlineKeyboardMarkup GetJournalRecorded()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", BotConstants.Payloads.NavJournal) }
        });
    }

    // Not implemented feature
    public static InlineKeyboardMarkup GetNotImplementedFeature()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", BotConstants.Payloads.NavRoot) }
        });
    }
}